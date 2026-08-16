-- Eventos: qué se creó, modificó o eliminó, quién y cuándo.
--
-- Es distinta de AuditLogs, que registra accesos a datos de salud (quién vio qué estudio).
-- Acá van los cambios sobre los datos mismos.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/005-eventos.sql

BEGIN;

CREATE TABLE IF NOT EXISTS "Eventos" (
    "Id"           uuid                     NOT NULL,
    "Entidad"      character varying(80)    NOT NULL,
    "EntidadId"    character varying(64)    NOT NULL,
    "Operacion"    character varying(20)    NOT NULL,
    -- Sin FK a Usuarios a propósito: la bitácora tiene que sobrevivir al borrado del usuario.
    -- Por eso también se guarda el email desnormalizado.
    "UsuarioId"    uuid,
    "UsuarioEmail" character varying(256),
    "Cambios"      jsonb,
    "Timestamp"    timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Eventos" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Eventos_Timestamp" ON "Eventos" ("Timestamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_Eventos_Entidad_Timestamp" ON "Eventos" ("Entidad", "Timestamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_Eventos_UsuarioId" ON "Eventos" ("UsuarioId");
CREATE INDEX IF NOT EXISTS "IX_Eventos_Operacion" ON "Eventos" ("Operacion");

COMMIT;
