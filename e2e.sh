#!/usr/bin/env bash
# Prueba de contrato de la serie «Crear base API».
# Corre el mismo recorrido contra cualquier stack: ./e2e.sh http://localhost:8080
# Necesita curl, jq y Mailpit escuchando en http://localhost:8025.
set -euo pipefail
API=${1:-http://localhost:8080}
MAIL=${MAILPIT:-http://localhost:8025}
EMAIL="ana.$(date +%s)@correo.com"
PASS="secreta123"
ok()   { printf '  \033[32m✔\033[0m %s\n' "$1"; }
fail() { printf '  \033[31m✘\033[0m %s\n' "$1"; exit 1; }
expect() { [ "$1" = "$2" ] && ok "$3 ($1)" || fail "$3: esperaba $2 y llegó $1"; }

# status <método> <ruta> <json> [token]  -> imprime el código y deja el cuerpo en /tmp/e2e-body
status() {
  local auth=()
  [ -n "${4:-}" ] && auth=(-H "Authorization: Bearer $4")
  curl -s -o /tmp/e2e-body -w '%{http_code}' -X "$1" "$API$2" \
    -H 'Content-Type: application/json' "${auth[@]}" ${3:+-d "$3"}
}

# Saca el token del último correo que recibió $EMAIL con el asunto dado.
token_from_mail() {
  sleep 1
  local id
  id=$(curl -s "$MAIL/api/v1/search?query=to:$EMAIL%20subject:\"$1\"" | jq -r '.messages[0].ID')
  curl -s "$MAIL/api/v1/message/$id" | jq -r '.Text' | grep -o 'token=[A-Za-z0-9_-]*' | head -1 | cut -d= -f2
}

echo "Contrato contra $API"
expect "$(status GET /health)" 200 "health"
expect "$(status POST /api/v1/auth/register "{\"email\":\"$EMAIL\",\"password\":\"$PASS\",\"name\":\"Ana\"}")" 201 "registro"
expect "$(status POST /api/v1/auth/register "{\"email\":\"$EMAIL\",\"password\":\"$PASS\",\"name\":\"Ana\"}")" 409 "registro duplicado"
expect "$(status POST /api/v1/auth/register '{"email":"malo","password":"123","name":""}')" 400 "registro inválido"
grep -q '"status"' /tmp/e2e-body && ok "el error trae Problem Details" || fail "el 400 no es Problem Details"
expect "$(status POST /api/v1/auth/login "{\"email\":\"$EMAIL\",\"password\":\"$PASS\"}")" 403 "login sin verificar"

VERIFY=$(token_from_mail "confirma")
[ -n "$VERIFY" ] && ok "llegó el correo de bienvenida" || fail "no llegó el correo de bienvenida"
expect "$(status POST /api/v1/auth/verify-email "{\"token\":\"$VERIFY\"}")" 204 "verificar correo"
expect "$(status POST /api/v1/auth/verify-email "{\"token\":\"$VERIFY\"}")" 400 "token de verificación reusado"

expect "$(status POST /api/v1/auth/login "{\"email\":\"$EMAIL\",\"password\":\"mala-clave\"}")" 401 "login con clave mala"
expect "$(status POST /api/v1/auth/login "{\"email\":\"$EMAIL\",\"password\":\"$PASS\"}")" 200 "login"
ACCESS=$(jq -r .accessToken /tmp/e2e-body); REFRESH=$(jq -r .refreshToken /tmp/e2e-body)

expect "$(status GET /api/v1/auth/me '' "$ACCESS")" 200 "me con token"
[ "$(jq -r .email /tmp/e2e-body)" = "$EMAIL" ] && ok "me devuelve el correo" || fail "me no devolvió el correo"
expect "$(status GET /api/v1/auth/me)" 401 "me sin token"

expect "$(status POST /api/v1/auth/refresh "{\"refreshToken\":\"$REFRESH\"}")" 200 "refresh"
NEW_REFRESH=$(jq -r .refreshToken /tmp/e2e-body)
expect "$(status POST /api/v1/auth/refresh "{\"refreshToken\":\"$REFRESH\"}")" 400 "refresh reusado"
expect "$(status POST /api/v1/auth/refresh "{\"refreshToken\":\"$NEW_REFRESH\"}")" 400 "reuso cerró la familia"

expect "$(status POST /api/v1/auth/login "{\"email\":\"$EMAIL\",\"password\":\"$PASS\"}")" 200 "login de nuevo"
ACCESS=$(jq -r .accessToken /tmp/e2e-body); REFRESH=$(jq -r .refreshToken /tmp/e2e-body)
expect "$(status POST /api/v1/auth/change-password '{"currentPassword":"mala","newPassword":"otraClave456"}' "$ACCESS")" 400 "cambiar clave con la actual mala"
expect "$(status POST /api/v1/auth/change-password "{\"currentPassword\":\"$PASS\",\"newPassword\":\"otraClave456\"}" "$ACCESS")" 204 "cambiar clave"
expect "$(status POST /api/v1/auth/refresh "{\"refreshToken\":\"$REFRESH\"}")" 400 "cambiar clave cerró sesiones"

expect "$(status POST /api/v1/auth/forgot-password '{"email":"nadie@correo.com"}')" 202 "olvidé clave (correo inexistente)"
expect "$(status POST /api/v1/auth/forgot-password "{\"email\":\"$EMAIL\"}")" 202 "olvidé clave"
RESET=$(token_from_mail "Restablece")
[ -n "$RESET" ] && ok "llegó el correo de restablecer" || fail "no llegó el correo de restablecer"
expect "$(status POST /api/v1/auth/reset-password "{\"token\":\"$RESET\",\"newPassword\":\"claveNueva789\"}")" 204 "restablecer clave"
expect "$(status POST /api/v1/auth/login "{\"email\":\"$EMAIL\",\"password\":\"claveNueva789\"}")" 200 "login con la clave nueva"
REFRESH=$(jq -r .refreshToken /tmp/e2e-body)
expect "$(status POST /api/v1/auth/logout "{\"refreshToken\":\"$REFRESH\"}")" 204 "logout"
expect "$(status POST /api/v1/auth/refresh "{\"refreshToken\":\"$REFRESH\"}")" 400 "refresh después de logout"
echo "Listo: el contrato se cumple."
