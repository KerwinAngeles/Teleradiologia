-- Urgencia y plazo de entrega. El estudio deja de ser "pendiente o no" y pasa a tener un reloj:
-- la plataforma vende tiempo de respuesta, así que el plazo tiene que ser un dato, no una promesa.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/005-prioridad-y-sla.sql

BEGIN;

-- Minutos de plazo por prioridad. NULL = se usa el valor global de configuración;
-- cada hospital puede tener su propio contrato.
ALTER TABLE "Hospitales" ADD COLUMN IF NOT EXISTS "SlaStatMinutos"    integer;
ALTER TABLE "Hospitales" ADD COLUMN IF NOT EXISTS "SlaUrgenteMinutos" integer;
ALTER TABLE "Hospitales" ADD COLUMN IF NOT EXISTS "SlaRutinaMinutos"  integer;

ALTER TABLE "Estudios" ADD COLUMN IF NOT EXISTS "Prioridad" character varying(20);
UPDATE "Estudios" SET "Prioridad" = 'Rutina' WHERE "Prioridad" IS NULL;
ALTER TABLE "Estudios" ALTER COLUMN "Prioridad" SET NOT NULL;

-- Momento en que vence el plazo. Se calcula al recibir el estudio y no se recalcula después:
-- si mañana cambia el contrato, los estudios viejos conservan el plazo con el que entraron.
ALTER TABLE "Estudios" ADD COLUMN IF NOT EXISTS "FechaLimite" timestamp with time zone;

-- Para medir cuánto tardó en tomarse y cuánto en informarse, por separado.
ALTER TABLE "Estudios" ADD COLUMN IF NOT EXISTS "AsignadoAt" timestamp with time zone;
ALTER TABLE "Estudios" ADD COLUMN IF NOT EXISTS "InformadoAt" timestamp with time zone;

-- Datos existentes: plazo de rutina (24 h) desde que entraron.
UPDATE "Estudios" SET "FechaLimite" = "CreatedAt" + interval '1440 minutes' WHERE "FechaLimite" IS NULL;

-- Los que ya están informados: se toma la firma del informe original como cierre.
UPDATE "Estudios" e
SET "InformadoAt" = i."FirmadoAt"
FROM "Informes" i
WHERE i."EstudioId" = e."Id"
  AND i."InformeAnteriorId" IS NULL
  AND i."FirmadoAt" IS NOT NULL
  AND e."InformadoAt" IS NULL;

ALTER TABLE "Estudios" ALTER COLUMN "FechaLimite" SET NOT NULL;

-- La worklist ordena por prioridad y vencimiento, y filtra por estado.
CREATE INDEX IF NOT EXISTS "IX_Estudios_Estado_FechaLimite" ON "Estudios" ("Estado", "FechaLimite");
CREATE INDEX IF NOT EXISTS "IX_Estudios_Prioridad" ON "Estudios" ("Prioridad");

COMMIT;
