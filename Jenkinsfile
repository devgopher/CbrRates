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
            defaultValue: 'http://host.docker.internal:8080',
            description: 'Smoke base URL from Jenkins container (Docker Desktop: host.docker.internal)'
        )
        booleanParam(
            name: 'KEEP_CONTAINERS',
            defaultValue: true,
            description: 'Leave app compose stack running after success. Uncheck to tear down.'
        )
    }

    environment {
        COMPOSE_PROJECT_NAME = 'cbrratesservice'
        COMPOSE_FILE = 'docker-compose.yml'
        // Runtime override only (do not confuse with docker-compose.jenkins.yml which runs Jenkins itself)
        COMPOSE_OVERRIDE = 'docker-compose.pipeline-env.yml'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO = '1'
        SOLUTION = 'CbrRatesService.sln'
    }

    stages {
        stage('Checkout') {
            when {
                // Multibranch / Pipeline from SCM already fills the workspace
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

        stage('Docker preflight') {
            steps {
                sh '''
                    set -e
                    if ! command -v docker >/dev/null 2>&1; then
                      echo "ERROR: docker CLI not found inside the Jenkins agent/container." >&2
                      echo "Jenkins runs in Docker but has no Docker CLI / no docker.sock mount (Docker-out-of-Docker)." >&2
                      echo "" >&2
                      echo "Fix on Win11 Docker Desktop (from repo root CbrRates):" >&2
                      echo "  docker compose -f docker-compose.jenkins.yml up -d --build" >&2
                      echo "Then open http://localhost:8081 and re-run the job." >&2
                      echo "That compose file mounts /var/run/docker.sock and builds Dockerfile.jenkins (docker + compose + dotnet SDK 8)." >&2
                      exit 1
                    fi
                    if [ ! -S /var/run/docker.sock ] && [ -z "$DOCKER_HOST" ]; then
                      echo "WARN: /var/run/docker.sock missing and DOCKER_HOST unset — docker commands will likely fail." >&2
                      echo "Mount the Docker Desktop socket: -v /var/run/docker.sock:/var/run/docker.sock" >&2
                    fi
                    docker version
                    docker compose version
                '''
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
                    sh '''
                        if command -v docker >/dev/null 2>&1; then
                          docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" down --remove-orphans \
                            || docker compose -f "$COMPOSE_FILE" down --remove-orphans \
                            || true
                        else
                          echo "Skip compose down: docker CLI not available"
                        fi
                    '''
                } else {
                    echo "Containers left running (${params.CONFIGURATION} / ASPNETCORE_ENVIRONMENT=${env.ASPNETCORE_ENVIRONMENT}). Host API: http://localhost:8080 | stop: docker compose down"
                }
            }
        }
        failure {
            sh '''
                if command -v docker >/dev/null 2>&1; then
                  docker compose -f "$COMPOSE_FILE" -f "$COMPOSE_OVERRIDE" logs --no-color --tail=200 \
                    || docker compose -f "$COMPOSE_FILE" logs --no-color --tail=200 \
                    || true
                else
                  echo "Skip compose logs: docker CLI not available (see Docker preflight / docker-compose.jenkins.yml)"
                fi
            '''
        }
        cleanup {
            sh 'rm -f "$COMPOSE_OVERRIDE" || true'
        }
    }
}
