# 🌌 Void - Chat App (v0.8.5)
# Documentação, Logs e Hotfixes

---

## ✨ Funcionalidades Atuais
* **Sistema de Identidade Dupla:** Diferenciação entre Nickname (apelido) e @Username (ID único).
* **Registro Inteligente:** Preenchimento automático de apelido se o campo for ignorado.
* **Persistência de Dados Local:** Contas salvas em JSON na pasta `/Accounts`.
* **ID Sequencial:** Controle global de IDs de usuários via `last_id.txt`.
* **Servidores e Canais:** Suporte a múltiplos servidores com auto-join no Servidor Oficial.
* **Configuração Local:** Edição do servidor padrão direto no `official_server.json`.

---

## 📝 Logs de Desenvolvimento (v0.8.5)
* **Identity Update:** Implementada a lógica de separação entre nome de exibição e nome único de usuário.
* **Infraestrutura:** Criação automática de pastas e arquivos de controle de ID no primeiro boot.
* **UI reativa:** Implementado o sistema de Toggle (IsVisible) para alternar entre Login e Registro sem trocar de janela.
* **Binding Sync:** Sincronização de propriedades do chat para suportar Nicknames e Timestamps.

---

## 🔥 Hotfix Report (Correções Aplicadas)
1. **[FIX] Erro AVLN:0004 (Padding):** Removida a propriedade Padding de StackPanels (não suportada) e movida para elementos Border.
2. **[FIX] Sincronização Nickname:** Corrigido o erro de binding onde o app procurava 'Name' mas o sistema usava 'Nickname'.
3. **[FIX] Parâmetros de Comando:** Atualizada a assinatura do `SelectServerCommand` no MainViewModel para aceitar o objeto `ServerItem` corretamente.
4. **[FIX] Renderização de Texto:** Adicionado DataTemplate no XAML para impedir que as mensagens aparecessem como o nome da classe (`Void.Models.MessageItem`).

---

## 🛠️ Tecnologias
* **Linguagem:** C# / .NET 8
* **Interface:** Avalonia UI
* **Arquitetura:** MVVM (CommunityToolkit.Mvvm)
* **Banco de Dados:** JSON Estático

---

## 🚀 Como Rodar
1. Certifique-se de ter o .NET 8 SDK instalado.
2. No terminal, use `dotnet restore` para baixar as dependências.
3. Use `dotnet run` para iniciar o Void.

---
"No Void, o silêncio é apenas o começo da conversa." 🌌
