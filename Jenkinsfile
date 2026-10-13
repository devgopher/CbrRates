// CbrRates — Declarative Pipeline (Windows agent primary + Docker Desktop)
// Repo: https://github.com/devgopher/CbrRates
// Flow: checkout → restore → build → test → docker compose up → smoke → optional teardown
//
// Dev = MSBuild Debug + ASPNETCORE_ENVIRONMENT=Development
// Deploy: docker-compose.yml (web:8080 + redis)
// Agent: native Windows Jenkins uses bat; Linux agents use sh (isUnix()).

pipeline {
    agent any

    options {
        timestamps()
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '20'))
    }

    parameters {
        choice(
            name: 'CONFIGURATION',
            choices: ['Dev', 'Release', 'Production'],
            description: 'Dev = Debug + ASPNETCORE_ENVIRONMENT=Development; Release/Production = Release + Production'
        )
        string(
            name: 'GIT_URL',
            defaultValue: 'https://github.com/devgopher/CbrRates.git',
            description: 'Git repo (only when not Multibranch / Pipeline from SCM)'
        )
        string(
            name: 'GIT_BRANCH',
            defaultValue: 'main',
            description: 'Branch to build (non-Multibranch)'
        )
        string(
            name: 'SMOKE_BASE_URL',
            defaultValue: 'http://localhost:8080',
            description: 'Smoke base URL (native Win Jenkins + Docker Desktop: http://localhost:8080)'
        )
        booleanParam(
            name: 'KEEP_CONTAINERS',
            defaultValue: true,
            description: 'Leave compose stack running after success. Uncheck to docker compose down.'
        )
    }

    environment {
        COMPOSE_PROJECT_NAME = 'cbrratesservice'
        COMPOSE_FILE = 'docker-compose.yml'
        COMPOSE_OVERRIDE = 'docker-compose.pipeline-env.yml'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO = '1'
        SOLUTION = 'CbrRatesService.sln'
    }

    stages {
        stage('Checkout') {
            when {
                expression { return !(env.BRANCH_NAME?.trim()) }
            }
            steps {
                checkout([
                    $class: 'GitSCM',
                    branches: [[name: "*/${params.GIT_BRANCH}"]],
                    extensions: [[$class: 'CleanBeforeCheckout']],
                    userRemoteConfigs: [[url: params.GIT_URL]]
                ])
            }
        }

        stage('Resolve configuration') {
            steps {
                script {
                    switch (params.CONFIGURATION) {
                        case 'Dev':
                            env.DOTNET_CONFIGURATION = 'Debug'
                            env.ASPNETCORE_ENVIRONMENT = 'Development'
                            break
                        case 'Release':
                        case 'Production':
                            env.DOTNET_CONFIGURATION = 'Release'
                            env.ASPNETCORE_ENVIRONMENT = 'Production'
                            break
                        default:
                            error "Unknown CONFIGURATION: ${params.CONFIGURATION}"
                    }
                    echo "Selected: ${params.CONFIGURATION} -> dotnet -c ${env.DOTNET_CONFIGURATION}, ASPNETCORE_ENVIRONMENT=${env.ASPNETCORE_ENVIRONMENT}"
                }
            }
        }

        stage('Restore') {
            steps {
                script {
                    if (isUnix()) {
                        sh 'dotnet --info'
                        sh 'dotnet restore "$SOLUTION"'
                    } else {
                        bat 'dotnet --info'
                        bat 'dotnet restore %SOLUTION%'
                    }
                }
            }
        }

        stage('Build') {
            steps {
                script {
                    if (isUnix()) {
                        sh 'dotnet build "$SOLUTION" -c "$DOTNET_CONFIGURATION" --no-restore'
                    } else {
                        bat 'dotnet build %SOLUTION% -c %DOTNET_CONFIGURATION% --no-restore'
                    }
                }
            }
        }

        stage('Test') {
            steps {
                script {
                    if (isUnix()) {
                        sh '''
                            rm -rf TestResults
                            mkdir -p TestResults
                            dotnet test "$SOLUTION" -c "$DOTNET_CONFIGURATION" --no-build --verbosity normal \
                              --logger "trx;LogFileName=results.trx" \
                              --results-directory "$(pwd)/TestResults"
                        '''
                    } else {
                        bat 'if exist TestResults rmdir /s /q TestResults'
                        bat 'mkdir TestResults'
                        bat 'dotnet test %SOLUTION% -c %DOTNET_CONFIGURATION% --no-build --verbosity normal --logger "trx;LogFileName=results.trx" --results-directory "%CD%\\TestResults"'
                    }
                }
            }
            post {
                always {
                    archiveArtifacts artifacts: 'TestResults/**/*.trx', allowEmptyArchive: true
                }
            }
        }

        stage('Docker preflight') {
            steps {
                script {
                    if (isUnix()) {
                        sh '''
                            set -e
                            if ! command -v docker >/dev/null 2>&1; then
                              echo "ERROR: docker CLI not found." >&2
                              echo "For Jenkins-in-Docker see docker-compose.jenkins.yml" >&2
                              exit 1
                            fi
                            docker version
                            docker compose version
                        '''
                    } else {
                        bat '''
                            where docker >nul 2>&1
                            if errorlevel 1 (
                              echo ERROR: docker CLI not found on PATH.
                              echo Install Docker Desktop and ensure "docker" works in a normal cmd.exe.
                              echo Optional Jenkins-in-Docker helper: docker-compose.jenkins.yml
                              exit /b 1
                            )
                            docker version
                            docker compose version
                        '''
                    }
                }
            }
        }

        stage('Docker Deploy') {
            steps {
                script {
                    if (isUnix()) {
                        sh '''
                            cat > "$COMPOSE_OVERRIDE" <<EOF
services:
  web:
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
EOF
                            cat "$COMPOSE_OVERRIDE"
                            docker pull mcr.microsoft.com/dotnet/sdk:8.0.425 || true
                            docker pull mcr.microsoft.com/dotnet/aspnet:8.0.22 || true
                            docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" down --remove-orphans || true
                            docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" build --pull --no-cache
                            docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" up -d
                        '''
                        sh '''
                            set -e
                            ok=0
                            for i in $(seq 1 40); do
                              status=$(docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" ps --format '{{.Service}} {{.Health}}' 2>/dev/null || true)
                              echo "[$i] $status"
                              if echo "$status" | grep -Eq 'web[[:space:]]+healthy'; then ok=1; break; fi
                              sleep 5
                            done
                            if [ "$ok" -ne 1 ]; then
                              docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" ps || true
                              docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" logs --no-color --tail=100 || true
                              echo "Timed out waiting for web healthcheck" >&2
                              exit 1
                            fi
                            echo "Web service is healthy."
                        '''
                        sh '''
                            set -e
                            url="${SMOKE_BASE_URL}/api/v1/rates"
                            echo "Smoke GET $url"
                            code=$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 10 --max-time 60 "$url" || true)
                            echo "Smoke HTTP $code"
                            case "$code" in 200|204) ;; *) echo "Unexpected status $code" >&2; exit 1 ;; esac
                        '''
                    } else {
                        bat '''
                            (
                            echo services:
                            echo   web:
                            echo     environment:
                            echo       ASPNETCORE_ENVIRONMENT: %ASPNETCORE_ENVIRONMENT%
                            ) > %COMPOSE_OVERRIDE%
                            type %COMPOSE_OVERRIDE%
                        '''
                        bat '''
                            docker pull mcr.microsoft.com/dotnet/sdk:8.0.425
                            docker pull mcr.microsoft.com/dotnet/aspnet:8.0.22
                            docker compose -f %COMPOSE_FILE% -f %COMPOSE_OVERRIDE% down --remove-orphans
                            docker compose -f %COMPOSE_FILE% -f %COMPOSE_OVERRIDE% build --pull --no-cache
                            docker compose -f %COMPOSE_FILE% -f %COMPOSE_OVERRIDE% up -d
                        '''
                        // Wait for compose healthcheck + smoke on localhost:8080
                        bat '''
                            powershell -NoProfile -Command "$ErrorActionPreference='Stop'; $ok=$false; for ($i=1; $i -le 40; $i++) { $lines = @(docker compose -f $env:COMPOSE_FILE -f $env:COMPOSE_OVERRIDE ps --format '{{.Service}} {{.Health}}' 2>$null) -join ' '; Write-Host ('[{0}] {1}' -f $i, $lines); if ($lines -match 'web\\s+healthy') { $ok=$true; break }; Start-Sleep -Seconds 5 }; if (-not $ok) { docker compose -f $env:COMPOSE_FILE -f $env:COMPOSE_OVERRIDE ps; docker compose -f $env:COMPOSE_FILE -f $env:COMPOSE_OVERRIDE logs --no-color --tail=100; throw 'Timed out waiting for web healthcheck' }; Write-Host 'Web service is healthy.'"
                        '''
                        bat '''
                            powershell -NoProfile -Command "$url = ($env:SMOKE_BASE_URL.TrimEnd('/') + '/api/v1/rates'); Write-Host ('Smoke GET {0}' -f $url); $code = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 60).StatusCode; Write-Host ('Smoke HTTP {0}' -f $code); if ($code -notin 200,204) { throw ('Unexpected status {0}' -f $code) }"
                        '''
                    }
                }
            }
        }
    }

    post {
        always {
            script {
                if (!params.KEEP_CONTAINERS) {
                    if (isUnix()) {
                        sh '''
                            if command -v docker >/dev/null 2>&1; then
                              docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" down --remove-orphans || docker compose -f "$COMPOSE_FILE" down --remove-orphans || true
                            else
                              echo "Skip compose down: docker CLI not available"
                            fi
                        '''
                    } else {
                        bat '''
                            where docker >nul 2>&1
                            if errorlevel 1 (
                              echo Skip compose down: docker CLI not available
                              exit /b 0
                            )
                            docker compose -f %COMPOSE_FILE% -f %COMPOSE_OVERRIDE% down --remove-orphans
                            if errorlevel 1 docker compose -f %COMPOSE_FILE% down --remove-orphans
                            exit /b 0
                        '''
                    }
                } else {
                    echo "Containers left running (${params.CONFIGURATION} / ASPNETCORE_ENVIRONMENT=${env.ASPNETCORE_ENVIRONMENT}). API: http://localhost:8080 | stop: docker compose -f docker-compose.yml down"
                }
            }
        }
        failure {
            script {
                if (isUnix()) {
                    sh '''
                        if command -v docker >/dev/null 2>&1; then
                          docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" logs --no-color --tail=200 || docker compose -f "$COMPOSE_FILE" logs --no-color --tail=200 || true
                        else
                          echo "Skip compose logs: docker CLI not available"
                        fi
                    '''
                } else {
                    bat '''
                        where docker >nul 2>&1
                        if errorlevel 1 (
                          echo Skip compose logs: docker CLI not available
                          exit /b 0
                        )
                        docker compose -f %COMPOSE_FILE% -f %COMPOSE_OVERRIDE% logs --no-color --tail=200
                        if errorlevel 1 docker compose -f %COMPOSE_FILE% logs --no-color --tail=200
                        exit /b 0
                    '''
                }
            }
        }
        cleanup {
            script {
                if (isUnix()) {
                    sh 'rm -f "$COMPOSE_OVERRIDE" || true'
                } else {
                    bat 'if exist %COMPOSE_OVERRIDE% del /f /q %COMPOSE_OVERRIDE%'
                }
            }
        }
    }
}
