#!/usr/bin/env bash
# Crea en Supabase las credenciales de los usuarios que venían de ASP.NET Core Identity.
# Los hashes de Identity no se pueden migrar, así que cada cuenta arranca con una contraseña
# nueva. El perfil local (Id, rol, estado) ya existe; el vínculo ProveedorUserId se completa
# solo en el primer login.
#
# Solo para el entorno de desarrollo local.
#
#   bash db/scripts/002-crear-credenciales-en-supabase.sh

set -euo pipefail

SUPABASE_URL="${SUPABASE_URL:-http://127.0.0.1:9999}"
SERVICE_ROLE_KEY="${SERVICE_ROLE_KEY:-eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJvbGUiOiJzZXJ2aWNlX3JvbGUiLCJpYXQiOjE3ODY3NjMzMTgsImV4cCI6MTk0NDQ0MzMxOH0.BZ_EHadR7jmmWtannqkV0MIYnO85CECOdeMOPZ4fBnQ}"

crear() {
  local email="$1" password="$2"
  local codigo
  codigo=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$SUPABASE_URL/admin/users" \
    -H "Authorization: Bearer $SERVICE_ROLE_KEY" \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"$email\",\"password\":\"$password\",\"email_confirm\":true}")

  case "$codigo" in
    200|201) echo "  creado    $email" ;;
    422)     echo "  ya existe $email" ;;
    *)       echo "  ERROR($codigo) $email" ;;
  esac
}

echo "Creando credenciales en $SUPABASE_URL"
crear admin@teleradiologia.local admin1234
crear carlos@radiologos.local    radiologo123
crear valentina@radiologos.local valentina123
crear sofia@radiologos.local     sofia1234
crear ana@hospitalcentral.local  tecnico123
