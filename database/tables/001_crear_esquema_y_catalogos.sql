create schema if not exists finanzas;

create table finanzas.tbt_categ_gasto
(
    idd_categ_gasto integer generated always as identity,
    nom_categ_gasto varchar(100) not null,
    est_categ_gasto boolean not null default true,
    fec_regis timestamptz not null default now(),

    constraint pk_tbt_categ_gasto
        primary key (idd_categ_gasto),

    constraint uq_tbt_categ_gasto_nombre
        unique (nom_categ_gasto)
);

create table finanzas.tbt_medio_pago
(
    idd_medio_pago integer generated always as identity,
    nom_medio_pago varchar(50) not null,
    est_medio_pago boolean not null default true,
    fec_regis timestamptz not null default now(),

    constraint pk_tbt_medio_pago
        primary key (idd_medio_pago),

    constraint uq_tbt_medio_pago_nombre
        unique (nom_medio_pago)
);

create table finanzas.tbt_estad_gasto
(
    cod_estado varchar(20) not null,
    nom_estado varchar(50) not null,
    est_estado boolean not null default true,

    constraint pk_tbt_estad_gasto
        primary key (cod_estado)
);

/*=========================================================
  STORAGE - COMPROBANTES
=========================================================*/

create policy "Usuario consulta sus comprobantes"
on storage.objects
for select
to authenticated
using
(
    bucket_id = 'comprobantes-gastos'
    and (storage.foldername(name))[1] = auth.uid()::text
);

create policy "Usuario carga sus comprobantes"
on storage.objects
for insert
to authenticated
with check
(
    bucket_id = 'comprobantes-gastos'
    and (storage.foldername(name))[1] = auth.uid()::text
);

create policy "Usuario actualiza sus comprobantes"
on storage.objects
for update
to authenticated
using
(
    bucket_id = 'comprobantes-gastos'
    and (storage.foldername(name))[1] = auth.uid()::text
)
with check
(
    bucket_id = 'comprobantes-gastos'
    and (storage.foldername(name))[1] = auth.uid()::text
);

create policy "Usuario elimina sus comprobantes"
on storage.objects
for delete
to authenticated
using
(
    bucket_id = 'comprobantes-gastos'
    and (storage.foldername(name))[1] = auth.uid()::text
);