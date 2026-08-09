create table if not exists finanzas.tbr_usuario_telegram (
 idd_usuario uuid primary key references auth.users(id) on delete cascade,
 idd_telegram bigint not null unique,
 idd_chat_telegram bigint not null unique,
 nom_usuario_telegram varchar(100) null,
 est_vinculado boolean not null default true,
 fec_vinculacion timestamptz not null default now(),
 fec_actualizacion timestamptz null
);
