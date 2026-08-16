alter table finanzas.tbt_gasto_pendiente_telegram enable row level security;

drop policy if exists "Usuarios gestionan sus gastos pendientes Telegram" on finanzas.tbt_gasto_pendiente_telegram;
create policy "Usuarios gestionan sus gastos pendientes Telegram"
on finanzas.tbt_gasto_pendiente_telegram
for all
to authenticated
using (auth.uid() = idd_usuario)
with check (auth.uid() = idd_usuario);
