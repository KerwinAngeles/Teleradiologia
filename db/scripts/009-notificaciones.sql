-- Notificaciones para el radiólogo. Se persisten además de mandarse por SignalR: si el
-- radiólogo no tenía la pantalla abierta, el aviso lo tiene que encontrar igual al entrar.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/009-notificaciones.sql

BEGIN;

CREATE TABLE IF NOT EXISTS "Notificaciones" (
    "Id"         uuid                     NOT NULL,
    "UsuarioId"  uuid                     NOT NULL,
    "Tipo"       character varying(40)    NOT NULL,
    "Titulo"     character varying(200)   NOT NULL,
    "Mensaje"    character varying(500)   NOT NULL,
    "EstudioId"  uuid,
    "HospitalId" uuid,
    "LeidaAt"    timestamp with time zone,
    "CreatedAt"  timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Notificaciones" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notificaciones_Usuarios_UsuarioId" FOREIGN KEY ("UsuarioId")
        REFERENCES "Usuarios" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notificaciones_Estudios_EstudioId" FOREIGN KEY ("EstudioId")
        REFERENCES "Estudios" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notificaciones_Hospitales_HospitalId" FOREIGN KEY ("HospitalId")
        REFERENCES "Hospitales" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Notificaciones_UsuarioId_CreatedAt" ON "Notificaciones" ("UsuarioId", "CreatedAt" DESC);
-- Parcial: el contador de no leídas es la consulta más frecuente.
CREATE INDEX IF NOT EXISTS "IX_Notificaciones_NoLeidas" ON "Notificaciones" ("UsuarioId") WHERE "LeidaAt" IS NULL;
CREATE INDEX IF NOT EXISTS "IX_Notificaciones_EstudioId" ON "Notificaciones" ("EstudioId");

COMMIT;
