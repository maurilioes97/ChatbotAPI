# Chatbot Frontend

Frontend React + TypeScript para a API ChatbotAPI.

## Instalação

1. **Instale as dependências:**
```bash
npm install
```

2. **Configure as variáveis de ambiente:**

Crie um arquivo `.env` baseado em `.env.example`:
```bash
cp .env.example .env
```

Atualize a URL da API se necessário:
```
VITE_API_BASE_URL=http://localhost:5000/api
```

## Desenvolvimento

Execute o servidor de desenvolvimento:
```bash
npm run dev
```

A aplicação será aberta em `http://localhost:5173`

## Build

Para criar uma build otimizada:
```bash
npm run build
```

## Estrutura de Pastas

```
src/
├── components/          # Componentes React
│   ├── ChatWindow.tsx   # Janela principal do chat
│   ├── MessageList.tsx  # Lista de mensagens
│   ├── MessageItem.tsx  # Item individual de mensagem
│   ├── InputArea.tsx    # Campo de entrada
│   ├── SessionList.tsx  # Sidebar com sessões
│   ├── SessionItem.tsx  # Item de sessão
│   └── NewSessionModal.tsx # Modal para criar sessão
├── hooks/               # Hooks customizados
│   └── useChatAPI.ts    # Hook para chamadas à API
├── types/               # Tipos TypeScript
│   └── chat.ts          # Interfaces de dados
├── config/              # Configurações
│   └── api.ts           # URL base da API
├── App.tsx              # Componente raiz
├── App.css              # Estilos globais (Tailwind)
└── main.tsx             # Entry point
```

## Tecnologias

- **React 18** - Framework UI
- **TypeScript** - Type safety
- **Vite** - Build tool rápido
- **Tailwind CSS** - Framework de estilos

## Recursos

✅ Múltiplas sessões de chat simultâneas
✅ Histórico de mensagens persistido localmente
✅ Interface responsiva e moderna
✅ Type-safe com TypeScript
✅ Componentes reutilizáveis

## Integração com API

A aplicação comunica com a API .NET Core nos endpoints:

- `POST /api/chat/nova-sessao` - Criar nova sessão
- `POST /api/chat/enviar-mensagem` - Enviar mensagem

Certifique-se que a API está rodando e CORS está configurado!
