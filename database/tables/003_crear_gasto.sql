create table finanzas.tbm_gasto
(
    idd_gasto bigint generated always as identity,
    idd_usuario uuid not null,
    idd_categ_gasto integer not null,
    idd_medio_pago integer,
    cod_estado varchar(20) not null default 'PENDIENTE',

    mon_gasto numeric(12,2) not null,
    fec_gasto timestamptz not null,

    nom_comercio varchar(150),
    des_gasto varchar(300),
    cod_origen varchar(20) not null,

    rut_comprobante varchar(500),
    nom_comprobante varchar(250),

    fec_regis timestamptz not null default now(),
    fec_actualizacion timestamptz,

    constraint pk_tbm_gasto
        primary key (idd_gasto),

    constraint fk_tbm_gasto_usuario
        foreign key (idd_usuario)
        references auth.users(id)
        on delete cascade,

    constraint fk_tbm_gasto_categoria
        foreign key (idd_categ_gasto)
        references finanzas.tbt_categ_gasto(idd_categ_gasto),

    constraint fk_tbm_gasto_medio_pago
        foreign key (idd_medio_pago)
        references finanzas.tbt_medio_pago(idd_medio_pago),

    constraint fk_tbm_gasto_estado
        foreign key (cod_estado)
        references finanzas.tbt_estad_gasto(cod_estado),

    constraint ck_tbm_gasto_monto
        check (mon_gasto > 0),

    constraint ck_tbm_gasto_origen
        check (
            cod_origen in ('TELEGRAM', 'WEB', 'APP')
        )
);

create index ix_tbm_gasto_usuario_fecha
on finanzas.tbm_gasto
(
    idd_usuario,
    fec_gasto desc
);

create index ix_tbm_gasto_usuario_categoria
on finanzas.tbm_gasto
(
    idd_usuario,
    idd_categ_gasto
);