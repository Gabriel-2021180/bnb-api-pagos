-- Usuario de la aplicación: solo puede conectarse y ejecutar los procedimientos del esquema payments.

SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'app_user', :'app_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_user')
\gexec

REVOKE ALL ON DATABASE :"db_name" FROM PUBLIC;
GRANT CONNECT ON DATABASE :"db_name" TO :"app_user";

REVOKE ALL ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON SCHEMA payments FROM PUBLIC;
GRANT USAGE ON SCHEMA payments TO :"app_user";

REVOKE ALL ON ALL TABLES IN SCHEMA payments FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA payments FROM :"app_user";

REVOKE ALL ON ALL FUNCTIONS IN SCHEMA payments FROM PUBLIC;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA payments TO :"app_user";

ALTER DEFAULT PRIVILEGES IN SCHEMA payments REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
