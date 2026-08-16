-- Firma manuscrita: el trazo que el radiólogo dibuja (o genera desde su nombre) al firmar.
--
-- Se guarda como PNG en data URL. La imagen entra en el payload firmado, así que no se puede
-- reemplazar por otra sin invalidar la firma.
--
-- VersionFirma existe porque el payload va a seguir cambiando: guardando con qué versión se
-- firmó, los informes viejos se siguen verificando con el formato que tenían.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/008-firma-manuscrita.sql

BEGIN;

ALTER TABLE "Informes" ADD COLUMN IF NOT EXISTS "FirmaImagen"  text;
ALTER TABLE "Informes" ADD COLUMN IF NOT EXISTS "VersionFirma" integer;

-- Lo ya firmado quedó con el payload v1 (sin imagen).
UPDATE "Informes" SET "VersionFirma" = 1 WHERE "Firma" IS NOT NULL AND "VersionFirma" IS NULL;

COMMIT;
