-- Transición de ASP.NET Core Identity a Supabase como proveedor de identidad,
-- y alta de las columnas de auditoría. Reforma la base en el lugar, preservando los datos.
--
-- Solo hace falta en bases creadas con las migraciones code first anteriores.
-- Una base nueva se crea directo con db/schema.sql, que ya refleja el estado final.
--
-- Los Id de Usuarios NO cambian: son el destino de 4 claves foráneas con datos clínicos.
--
--   psql -U teleradiologia -d teleradiologia -f db/scripts/001-migrar-a-supabase-auth.sql

BEGIN;

ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "Rol"             character varying(20);
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "EstadoAcceso"    character varying(20);
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "Proveedor"       character varying(50);
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "ProveedorUserId" character varying(128);
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "FechaDecision"   timestamp with time zone;
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "DecididoPorId"   uuid;
ALTER TABLE "Usuarios" ADD COLUMN IF NOT EXISTS "MotivoDecision"  character varying(500);

-- El rol vivía en la N:N de Identity; pasa a ser una columna.
UPDATE "Usuarios" u
SET "Rol" = COALESCE(
    (SELECT r."Name" FROM "UsuarioRoles" ur JOIN "Roles" r ON r."Id" = ur."RoleId" WHERE ur."UserId" = u."Id" LIMIT 1),
    'Tecnico');

-- Las cuentas que ya existían estaban en uso: quedan aprobadas, no pendientes.
UPDATE "Usuarios" SET "EstadoAcceso" = CASE WHEN "Activo" THEN 'Aprobado' ELSE 'Suspendido' END;
UPDATE "Usuarios" SET "Proveedor" = 'supabase';

-- ProveedorUserId queda NULL a propósito: los hashes de Identity no se pueden migrar.
-- Estos usuarios se vinculan al darlos de alta en Supabase (ver db/scripts/002).

ALTER TABLE "Usuarios" ALTER COLUMN "Email" SET NOT NULL;
ALTER TABLE "Usuarios" ALTER COLUMN "Rol" SET NOT NULL;
ALTER TABLE "Usuarios" ALTER COLUMN "EstadoAcceso" SET NOT NULL;
ALTER TABLE "Usuarios" ALTER COLUMN "Proveedor" SET NOT NULL;

DROP INDEX IF EXISTS "EmailIndex";
DROP INDEX IF EXISTS "UserNameIndex";

ALTER TABLE "Usuarios"
    DROP COLUMN IF EXISTS "UserName",
    DROP COLUMN IF EXISTS "NormalizedUserName",
    DROP COLUMN IF EXISTS "NormalizedEmail",
    DROP COLUMN IF EXISTS "EmailConfirmed",
    DROP COLUMN IF EXISTS "PasswordHash",
    DROP COLUMN IF EXISTS "SecurityStamp",
    DROP COLUMN IF EXISTS "ConcurrencyStamp",
    DROP COLUMN IF EXISTS "PhoneNumber",
    DROP COLUMN IF EXISTS "PhoneNumberConfirmed",
    DROP COLUMN IF EXISTS "TwoFactorEnabled",
    DROP COLUMN IF EXISTS "LockoutEnd",
    DROP COLUMN IF EXISTS "LockoutEnabled",
    DROP COLUMN IF EXISTS "AccessFailedCount",
    DROP COLUMN IF EXISTS "Activo";

ALTER TABLE "Usuarios" ADD CONSTRAINT "FK_Usuarios_Usuarios_DecididoPorId"
    FOREIGN KEY ("DecididoPorId") REFERENCES "Usuarios" ("Id") ON DELETE RESTRICT;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Usuarios_Email" ON "Usuarios" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Usuarios_ProveedorUserId" ON "Usuarios" ("ProveedorUserId")
    WHERE "ProveedorUserId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_Usuarios_EstadoAcceso" ON "Usuarios" ("EstadoAcceso");

DROP TABLE IF EXISTS "AspNetRoleClaims";
DROP TABLE IF EXISTS "AspNetUserClaims";
DROP TABLE IF EXISTS "AspNetUserLogins";
DROP TABLE IF EXISTS "AspNetUserTokens";
DROP TABLE IF EXISTS "UsuarioRoles";
DROP TABLE IF EXISTS "Roles";

-- Auditoría en las tablas del dominio. AuditLogs queda afuera: sus filas son inmutables.
ALTER TABLE "Usuarios"  ADD COLUMN IF NOT EXISTS "CreatedBy"      character varying(256);
ALTER TABLE "Usuarios"  ADD COLUMN IF NOT EXISTS "LastModified"   timestamp with time zone;
ALTER TABLE "Usuarios"  ADD COLUMN IF NOT EXISTS "LastModifiedBy" character varying(256);

ALTER TABLE "Pacientes" ADD COLUMN IF NOT EXISTS "CreatedBy"      character varying(256);
ALTER TABLE "Pacientes" ADD COLUMN IF NOT EXISTS "LastModified"   timestamp with time zone;
ALTER TABLE "Pacientes" ADD COLUMN IF NOT EXISTS "LastModifiedBy" character varying(256);

ALTER TABLE "Estudios"  ADD COLUMN IF NOT EXISTS "CreatedBy"      character varying(256);
ALTER TABLE "Estudios"  ADD COLUMN IF NOT EXISTS "LastModified"   timestamp with time zone;
ALTER TABLE "Estudios"  ADD COLUMN IF NOT EXISTS "LastModifiedBy" character varying(256);

ALTER TABLE "Informes"  ADD COLUMN IF NOT EXISTS "CreatedBy"      character varying(256);
ALTER TABLE "Informes"  ADD COLUMN IF NOT EXISTS "LastModified"   timestamp with time zone;
ALTER TABLE "Informes"  ADD COLUMN IF NOT EXISTS "LastModifiedBy" character varying(256);

ALTER TABLE "Estudios" ALTER COLUMN "SubidoPorId" DROP DEFAULT;

-- El esquema deja de ser responsabilidad de EF.
DROP TABLE IF EXISTS "__EFMigrationsHistory";

COMMIT;
