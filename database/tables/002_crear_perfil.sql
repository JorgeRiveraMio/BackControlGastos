create table finanzas.tbm_perfil
(
    idd_usuario uuid not null,
    nom_usuario varchar(120) not null,
    cod_moneda varchar(3) not null default 'PEN',
    zon_horaria varchar(50) not null default 'America/Lima',
    mon_ingreso_mensual numeric(12,2),
    mon_presupuesto_mensual numeric(12,2),
    por_alerta numeric(5,2) not null default 80,
    est_perfil boolean not null default true,
    fec_regis timestamptz not null default now(),
    fec_actualizacion timestamptz,

    constraint pk_tbm_perfil
        primary key (idd_usuario),

    constraint fk_tbm_perfil_usuario
        foreign key (idd_usuario)
        references auth.users(id)
        on delete cascade,

    constraint ck_tbm_perfil_ingreso
        check (
            mon_ingreso_mensual is null
            or mon_ingreso_mensual >= 0
        ),

    constraint ck_tbm_perfil_presupuesto
        check (
            mon_presupuesto_mensual is null
            or mon_presupuesto_mensual >= 0
        ),

    constraint ck_tbm_perfil_alerta
        check (
            por_alerta > 0
            and por_alerta <= 100
        )
);