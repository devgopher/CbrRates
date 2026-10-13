// CbrRates — Declarative Pipeline (Linux agent)
// Repo: https://github.com/devgopher/CbrRates
// Typical setup: Jenkins in Linux Docker on Win11 + Docker Desktop (docker.sock mounted)
// Tools on agent: git, dotnet SDK 8, docker, docker compose
//
// Dev = MSBuild Debug + ASPNETCORE_ENVIRONMENT=Development (no MSBuild "Development" config)

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
            description: 'Git repo (used when job is "Pipeline script", not Multibranch / Pipeline from SCM)'
        )
        string(
            name: 'GIT_BRANCH',
            defaultValue: 'main',
            description: 'Branch to build'
        )
        string(
            name: 'SMOKE_BASE_URL',
            defaultValue: 'http://host.docker.internal:8080',
            description: 'Base URL for smoke test from Jenkins container (Docker Desktop: host.docker.internal; same network: http://localhost:8080)'
        )
        booleanParam(
            name: 'KEEP_CONTAINERS',
            defaultValue: true,
            description: 'Leave compose stack running after success. Uncheck to tear down.'
        )
    }

    environment {
        COMPOSE_PROJECT_NAME = 'cbrratesservice'
        COMPOSE_FILE = 'docker-compose.yml'
        COMPOSE_OVERRIDE = 'docker-compose.jenkins.yml'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO = '1'
        SOLUTION = 'CbrRatesService.sln'
    }

    stages {
        stage('Checkout') {
            steps {
                // Works for pasted Pipeline script (unlike "checkout scm")
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
                sh 'dotnet --info'
                sh 'dotnet restore "$SOLUTION"'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build "$SOLUTION" -c "$DOTNET_CONFIGURATION" --no-restore'
            }
        }

        stage('Test') {
            steps {
                sh '''
                    rm -rf TestResults
                    mkdir -p TestResults
                    dotnet test "$SOLUTION" -c "$DOTNET_CONFIGURATION" --no-build --verbosity normal \
                      --logger "trx;LogFileName=results.trx" \
                      --results-directory "$(pwd)/TestResults"
                '''
            }
            post {
                always {
                    archiveArtifacts artifacts: 'TestResults/**/*.trx', allowEmptyArchive: true
                }
            }
        }

        stage('Docker Build & Run') {
            steps {
                sh '''
                    cat > "$COMPOSE_OVERRIDE" <<EOF
services:
  web:
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
EOF
                    cat "$COMPOSE_OVERRIDE"
                '''
                sh '''
                    docker version
                    docker compose version
                    docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" down --remove-orphans || true
                    docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" up -d --build
                '''
                sh '''
                    set -e
                    ok=0
                    for i in $(seq 1 40); do
                      status=$(docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" ps --format '{{.Service}} {{.Health}}' 2>/dev/null || true)
                      echo "[$i] $status"
                      if echo "$status" | grep -Eq 'web[[:space:]]+healthy'; then
                        ok=1
                        break
                      fi
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
                    code=$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 10 --max-time 30 "$url" || true)
                    echo "Smoke HTTP $code"
                    case "$code" in
                      200|204) ;;
                      *) echo "Unexpected status $code" >&2; exit 1 ;;
                    esac
                '''
            }
        }
    }

    post {
        always {
            script {
                if (!params.KEEP_CONTAINERS) {
                    sh 'docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" down --remove-orphans || docker compose -f "$COMPOSE_FILE" down --remove-orphans || true'
                } else {
                    echo "Containers left running (${params.CONFIGURATION} / ASPNETCORE_ENVIRONMENT=${env.ASPNETCORE_ENVIRONMENT}). API on host: http://localhost:8080 | stop: docker compose down"
                }
            }
        }
        failure {
            sh 'docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" logs --no-color --tail=200 || docker compose -f "$COMPOSE_FILE" logs --no-color --tail=200 || true'
        }
        cleanup {
            sh 'rm -f "$COMPOSE_OVERRIDE" || true'
        }
    }
}
