alter table finanzas.tbt_vinculacion_telegram enable row level security;
drop policy if exists "Usuarios consultan sus vínculos Telegram" on finanzas.tbt_vinculacion_telegram;
create policy "Usuarios consultan sus vínculos Telegram" on finanzas.tbt_vinculacion_telegram for all to authenticated using (auth.uid()=idd_usuario) with check (auth.uid()=idd_usuario);
