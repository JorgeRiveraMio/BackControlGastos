alter table finanzas.tbt_gasto_pendiente_telegram
    add column if not exists idd_medio_pago integer null;

alter table finanzas.tbt_gasto_pendiente_telegram
    drop constraint if exists fk_tbt_gasto_pendiente_telegram_medio_pago;

alter table finanzas.tbt_gasto_pendiente_telegram
    add constraint fk_tbt_gasto_pendiente_telegram_medio_pago
    foreign key (idd_medio_pago)
    references finanzas.tbt_medio_pago(idd_medio_pago)
    on delete restrict;
