-- Prepara postgres:16-alpine para que GoTrue pueda correr sus migraciones.
-- La imagen supabase/postgres trae esto precreado; acá hay que hacerlo a mano.

CREATE SCHEMA IF NOT EXISTS auth AUTHORIZATION supabase_auth_admin;

-- La migración 20240612123726 hace GRANT ... TO postgres y falla si el rol no existe.
DO $$ BEGIN CREATE ROLE postgres SUPERUSER LOGIN; EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE ROLE anon NOLOGIN NOINHERIT; EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE ROLE authenticated NOLOGIN NOINHERIT; EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE ROLE service_role NOLOGIN NOINHERIT BYPASSRLS; EXCEPTION WHEN duplicate_object THEN NULL; END $$;
