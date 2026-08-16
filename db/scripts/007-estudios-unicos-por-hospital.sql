-- Los estudios pasaron a ser por hospital, pero sus índices únicos quedaron globales.
--
-- Consecuencia: subir un estudio que ya existía en OTRO hospital reventaba con
-- "duplicate key value violates unique constraint IX_Estudios_OrthancStudyId", porque la
-- comprobación de idempotencia no encontraba nada (el filtro de inquilino se lo ocultaba)
-- y el índice global sí lo veía.
--
-- Dos hospitales pueden recibir legítimamente el mismo estudio: son dos registros distintos,
-- con su propio contrato, su propio SLA y su propia trazabilidad. La unicidad es por hospital.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/007-estudios-unicos-por-hospital.sql

BEGIN;

DROP INDEX IF EXISTS "IX_Estudios_OrthancStudyId";
DROP INDEX IF EXISTS "IX_Estudios_StudyInstanceUid";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Estudios_HospitalId_OrthancStudyId"
    ON "Estudios" ("HospitalId", "OrthancStudyId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Estudios_HospitalId_StudyInstanceUid"
    ON "Estudios" ("HospitalId", "StudyInstanceUid");

-- Se siguen consultando sueltos al resolver un estudio por su identificador DICOM.
CREATE INDEX IF NOT EXISTS "IX_Estudios_StudyInstanceUid" ON "Estudios" ("StudyInstanceUid");

COMMIT;
