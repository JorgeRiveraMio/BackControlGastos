create table if not exists finanzas.tbt_gasto_pendiente_telegram (
    idd_gasto_pendiente bigint generated always as identity primary key,
    idd_usuario uuid not null references auth.users(id) on delete cascade,
    idd_chat_telegram bigint not null,
    mon_gasto numeric(12,2) not null check (mon_gasto > 0),
    des_gasto varchar(250) not null check (length(trim(des_gasto)) > 0),
    idd_categ_gasto integer not null references finanzas.tbt_categ_gasto(idd_categ_gasto),
    fec_gasto timestamptz not null,
    cod_estado varchar(20) not null check (cod_estado in ('PENDIENTE', 'CONFIRMADO', 'CANCELADO')),
    fec_expiracion timestamptz not null,
    fec_regis timestamptz not null default now(),
    fec_actualizacion timestamptz null,
    constraint ck_tbt_gasto_pendiente_telegram_expiracion check (fec_expiracion > fec_regis)
);

create index if not exists ix_tbt_gasto_pendiente_telegram_chat
    on finanzas.tbt_gasto_pendiente_telegram (idd_chat_telegram);
create index if not exists ix_tbt_gasto_pendiente_telegram_usuario
    on finanzas.tbt_gasto_pendiente_telegram (idd_usuario);
create index if not exists ix_tbt_gasto_pendiente_telegram_estado
    on finanzas.tbt_gasto_pendiente_telegram (cod_estado);
