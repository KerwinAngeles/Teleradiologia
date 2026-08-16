#!/usr/bin/env bash
# Puebla CatalogoEstablecimientos con el listado de centros de salud del MISPAS/SNS.
#
# Fuente: https://datos.gob.do/dataset/listado-de-centros-de-salud (licencia ODbL).
# ~1900 establecimientos de la red pública. Los privados no figuran: se dan de alta a mano.
#
#   bash db/scripts/004-importar-catalogo-msp.sh
#
# Es idempotente: se vuelve a correr para actualizar el catálogo cuando el MSP lo republique.

set -euo pipefail

CONTENEDOR="${CONTENEDOR_POSTGRES:-teleradiologia-postgres-1}"
DB_USER="${POSTGRES_USER:-teleradiologia}"
DB_NAME="${POSTGRES_DB:-teleradiologia}"

CSV_URL="${CSV_URL:-https://www.msp.gob.do/web/Transparencia/documentos_oai/745/establecimientos-de-salud-publicos/34505/establecimientos-de-salud-publicos-mispas-1878-2026-2.csv}"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"; docker exec "$CONTENEDOR" rm -f /tmp/catalogo-msp.csv >/dev/null 2>&1 || true' EXIT

echo "Descargando el catálogo…"
curl -sL --fail -A 'Mozilla/5.0' "$CSV_URL" -o "$TMP/centros.csv"

# El archivo viene en latin-1 y Postgres espera UTF-8.
if command -v iconv >/dev/null 2>&1; then
  iconv -f LATIN1 -t UTF-8 "$TMP/centros.csv" > "$TMP/centros.utf8.csv"
else
  python -c "import io,sys; io.open(sys.argv[2],'w',encoding='utf-8',newline='').write(io.open(sys.argv[1],encoding='latin-1').read())" \
    "$TMP/centros.csv" "$TMP/centros.utf8.csv"
fi

echo "  $(($(wc -l < "$TMP/centros.utf8.csv") - 1)) registros"

docker cp "$TMP/centros.utf8.csv" "$CONTENEDOR:/tmp/catalogo-msp.csv" >/dev/null

echo "Importando…"
docker exec -i "$CONTENEDOR" psql -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 <<'SQL'
BEGIN;

CREATE TEMP TABLE catalogo_staging (
    id_centro    text,
    nombre       text,
    nivel        text,
    tipo         text,
    region       text,
    provincia    text,
    municipio    text,
    anio         text
) ON COMMIT DROP;

\copy catalogo_staging FROM '/tmp/catalogo-msp.csv' WITH (FORMAT csv, DELIMITER ';', HEADER true)

INSERT INTO "CatalogoEstablecimientos"
    ("Codigo", "Nombre", "NivelAtencion", "Tipo", "RegionSalud", "Provincia", "Municipio", "AnioApertura")
SELECT
    id_centro::integer,
    btrim(nombre),
    NULLIF(btrim(nivel), ''),
    NULLIF(btrim(tipo), ''),
    NULLIF(btrim(region), ''),
    NULLIF(btrim(provincia), ''),
    NULLIF(btrim(municipio), ''),
    NULLIF(btrim(anio), '')::integer
FROM catalogo_staging
WHERE id_centro ~ '^[0-9]+$'
ON CONFLICT ("Codigo") DO UPDATE SET
    "Nombre"        = EXCLUDED."Nombre",
    "NivelAtencion" = EXCLUDED."NivelAtencion",
    "Tipo"          = EXCLUDED."Tipo",
    "RegionSalud"   = EXCLUDED."RegionSalud",
    "Provincia"     = EXCLUDED."Provincia",
    "Municipio"     = EXCLUDED."Municipio",
    "AnioApertura"  = EXCLUDED."AnioApertura";

COMMIT;

SELECT count(*) AS establecimientos FROM "CatalogoEstablecimientos";
SQL
