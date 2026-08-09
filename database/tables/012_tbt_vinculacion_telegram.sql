create table if not exists finanzas.tbt_vinculacion_telegram (
 idd_vinculacion bigint generated always as identity primary key,
 idd_usuario uuid not null references auth.users(id) on delete cascade,
 cod_token varchar(100) not null unique,
 fec_expiracion timestamptz not null,
 est_usado boolean not null default false,
 fec_regis timestamptz not null default now()
);
