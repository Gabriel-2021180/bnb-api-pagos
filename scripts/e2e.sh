#!/bin/bash
# Pruebas end-to-end contra la API levantada con "docker compose up -d --build".
# Uso: bash scripts/e2e.sh          (SHOW=1 bash scripts/e2e.sh para ver cada respuesta)
# Lee API_PORT, API_KEY, JWT_CLIENT_ID y JWT_CLIENT_SECRET desde .env.
cd "$(dirname "$0")/.." || exit 2
if [ -f .env ]; then set -a; . ./.env; set +a; fi
U="http://localhost:${API_PORT:-8080}"
KEY="X-Api-Key: ${API_KEY:?Defina API_KEY en .env}"
CLIENT_ID="${JWT_CLIENT_ID:-default-client}"
CLIENT_SECRET="${JWT_CLIENT_SECRET:?Defina JWT_CLIENT_SECRET en .env}"
CT="Content-Type: application/json"
C=cfe8b150-2f84-4a1a-bdf4-923b20e34973
D=$(mktemp -d)
pass=0; fail=0

check() { # nombre esperado obtenido cuerpo
  if [ "$2" == "$3" ]; then pass=$((pass+1)); printf "OK   %-45s %s\n" "$1" "$3";
  else fail=$((fail+1)); printf "FAIL %-45s esperado %s obtuvo %s\n     %s\n" "$1" "$2" "$3" "$4"; fi
  if [ -n "$SHOW" ] && [ -n "$4" ]; then printf "     %s\n" "$4"; fi
  return 0
}
req() { # nombre esperado args...
  local name=$1 exp=$2; shift 2
  local body code
  body=$(curl -s -o "$D/out" -w "%{http_code}" "$@"); code=$body; body=$(cat "$D/out")
  check "$name" "$exp" "$code" "$body"
}
json() { printf '%s' "$1" > "$D/b.json"; echo "@$D/b.json"; }

echo "== Salud y Swagger"
req "health/live" 200 $U/health/live
req "health/ready" 200 $U/health/ready
req "swagger UI" 200 $U/swagger/index.html
req "swagger.json" 200 $U/swagger/v1/swagger.json

echo "== Seguridad"
req "GET sin credenciales" 401 "$U/api/payments?customerId=$C"
req "GET API Key incorrecta" 401 "$U/api/payments?customerId=$C" -H "X-Api-Key: incorrecta"
req "GET Bearer inválido" 401 "$U/api/payments?customerId=$C" -H "Authorization: Bearer abc.def.ghi"
req "token secreto incorrecto" 401 -X POST $U/api/auth/token -H "$CT" --data-binary "$(json '{"clientId":"'"$CLIENT_ID"'","clientSecret":"secreto-incorrecto"}')"
req "token sin campos" 400 -X POST $U/api/auth/token -H "$CT" --data-binary "$(json '{}')"
TOKEN=$(curl -s -X POST $U/api/auth/token -H "$CT" --data-binary "$(json '{"clientId":"'"$CLIENT_ID"'","clientSecret":"'"$CLIENT_SECRET"'"}')" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')
[ -n "$TOKEN" ] && check "token emitido" ok ok || check "token emitido" ok vacio
req "GET con JWT" 200 "$U/api/payments?customerId=$C" -H "Authorization: Bearer $TOKEN"
# Se altera el payload (segundo segmento): la firma deja de coincidir.
IFS=. read -r h p s <<< "$TOKEN"
TAMPERED="$h.$(printf '%s' "$p" | sed 's/^eyJ/eyK/').$s"
req "GET con JWT alterado" 401 "$U/api/payments?customerId=$C" -H "Authorization: Bearer $TAMPERED"

echo "== Registro de pagos"
req "POST válido (API Key)" 201 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"SERVICIOS ELÉCTRICOS S.A.","amount":120.50,"currency":"BOB"}')"
req "POST válido (JWT), bob minúscula" 201 -X POST $U/api/payments -H "Authorization: Bearer $TOKEN" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"AGUA POTABLE","amount":1500,"currency":"bob"}')"
req "POST en dólares (USD)" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":10,"currency":"USD"}')"
req "POST otra moneda (EUR)" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":10,"currency":"EUR"}')"
req "POST sin moneda" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":10}')"
req "POST monto > 1500" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":1500.01,"currency":"BOB"}')"
req "POST monto 0" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":0,"currency":"BOB"}')"
req "POST monto negativo" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":-5,"currency":"BOB"}')"
req "POST 3 decimales" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":10.555,"currency":"BOB"}')"
req "POST sin customerId ni proveedor" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"amount":10,"currency":"BOB"}')"
req "POST customerId no es GUID" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"abc","serviceProvider":"X","amount":10,"currency":"BOB"}')"
req "POST amount texto" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":"'$C'","serviceProvider":"X","amount":"diez","currency":"BOB"}')"
req "POST JSON mal formado" 400 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "$(json '{"customerId":')"
req "POST body vacío" 400 -X POST $U/api/payments -H "$KEY" -H "$CT"
req "POST Content-Type text/plain" 415 -X POST $U/api/payments -H "$KEY" -H "Content-Type: text/plain" --data-binary "hola"
{ printf '{"customerId":"%s","serviceProvider":"' "$C"; head -c 40000 /dev/zero | tr '\0' 'a'; printf '","amount":10,"currency":"BOB"}'; } > "$D/big"
req "POST body > 32 KB" 413 -X POST $U/api/payments -H "$KEY" -H "$CT" --data-binary "@$D/big"

echo "== Consulta"
req "GET válido" 200 "$U/api/payments?customerId=$C" -H "$KEY"
req "GET cliente sin pagos" 200 "$U/api/payments?customerId=11111111-1111-1111-1111-111111111111" -H "$KEY"
req "GET sin customerId" 400 "$U/api/payments" -H "$KEY"
req "GET customerId inválido" 400 "$U/api/payments?customerId=abc" -H "$KEY"
req "GET customerId vacío (ceros)" 400 "$U/api/payments?customerId=00000000-0000-0000-0000-000000000000" -H "$KEY"

echo "== Rutas y métodos"
req "ruta inexistente" 404 $U/api/noexiste -H "$KEY"
req "método no permitido (DELETE)" 405 -X DELETE "$U/api/payments" -H "$KEY"

echo "== Cabeceras de seguridad"
H=$(curl -s -D - -o /dev/null "$U/api/payments?customerId=$C" -H "$KEY" | tr -d '\r')
for h in "X-Content-Type-Options: nosniff" "X-Frame-Options: DENY" "Content-Security-Policy" "Referrer-Policy: no-referrer" "Cache-Control: no-store"; do
  echo "$H" | grep -qi "^$h" && check "cabecera $h" si si || check "cabecera $h" si no
done
echo "$H" | grep -qi "^Server:" && check "sin cabecera Server" no si || check "sin cabecera Server" no no

echo
echo "Resultado: $pass OK, $fail FAIL"
rm -rf "$D"
[ "$fail" -eq 0 ]
