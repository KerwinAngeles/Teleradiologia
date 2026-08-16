-- Firma digital de informes.
--
-- Hasta ahora "firmado" era un estado y una fecha: si alguien editaba el contenido en la base,
-- el informe seguía figurando como firmado. Con el hash y la firma se puede probar que el texto
-- es exactamente el que el radiólogo firmó.
--
-- Los datos del firmante se copian al informe (no se leen del usuario) porque un documento
-- firmado no puede cambiar si después la persona corrige su nombre o su matrícula.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/006-firma-digital.sql

BEGIN;

ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "Matricula" character varying(50);

ALTER TABLE "Informes" ADD COLUMN IF NOT EXISTS "HashContenido"     character varying(64);
ALTER TABLE "Informes" ADD COLUMN IF NOT EXISTS "Firma"             text;
ALTER TABLE "Informes" ADD COLUMN IF NOT EXISTS "AlgoritmoFirma"    character varying(30);
ALTER TABLE "Informes" ADD COLUMN IF NOT EXISTS "FirmanteNombre"    character varying(200);
ALTER TABLE "Informes" ADD COLUMN IF NOT EXISTS "FirmanteMatricula" character varying(50);

CREATE INDEX IF NOT EXISTS "IX_Informes_HashContenido" ON "Informes" ("HashContenido");

COMMIT;
