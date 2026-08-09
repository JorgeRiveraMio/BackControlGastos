create table if not exists finanzas.tbt_alerta (
    idd_alerta bigint generated always as identity primary key,
    idd_usuario uuid not null references auth.users(id) on delete cascade,
    cod_tipo_alerta varchar(30) not null,
    des_alerta varchar(300) not null,
    por_umbral numeric(5,2) null,
    mon_presupuesto numeric(12,2) null,
    mon_gastado numeric(12,2) null,
    anio_periodo integer not null,
    mes_periodo integer not null check (mes_periodo between 1 and 12),
    est_leida boolean not null default false,
    fec_regis timestamptz not null default now(),
    constraint uq_tbt_alerta_usuario_tipo_periodo unique (idd_usuario, cod_tipo_alerta, anio_periodo, mes_periodo)
);
