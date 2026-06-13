# Guia Completo do Projeto ChatbotAPI

## Objetivo deste documento
- Este material foi criado para servir como roteiro de apresentacao em aula.
- A ideia e explicar a arquitetura, o fluxo do sistema e o papel de cada arquivo relevante do projeto.
- Foram ignoradas pastas geradas automaticamente como `bin`, `obj` e `node_modules`, porque elas nao representam logica de negocio.

## Visao geral da aplicacao
- O projeto e um chatbot com frontend em React e backend em ASP.NET Core Web API.
- O backend salva sessoes e mensagens no SQL Server usando Entity Framework Core.
- O backend tambem integra com a API Gemini para responder perguntas, transcrever audio, sugerir perguntas e gerar resumos.
- O frontend permite criar sessoes, enviar mensagens, anexar documentos, enviar audio e exportar resumo em PDF.

## Estrutura geral
- `ChatbotAPI.slnx`: arquivo de solucao que aponta para o projeto .NET principal.
- `docker-compose.yml`: sobe tres servicos juntos, banco SQL Server, backend e frontend.
- `ChatbotAPI/`: pasta do backend ASP.NET Core.
- `chatbot-frontend/`: pasta do frontend React + Vite.

## Fluxo funcional do sistema
- O usuario abre o frontend no navegador.
- O frontend cria sessoes e envia requisicoes HTTP para a API.
- O controller recebe a requisicao e delega para a camada de servico.
- O servico conversa com o banco e, quando necessario, com o Gemini.
- O resultado volta para o controller e depois para o frontend.
- O frontend atualiza a interface, o historico local e os estados visuais.

## Arquivos de raiz

## `ChatbotAPI.slnx`
- Define a solucao do Visual Studio / .NET.
- Referencia o projeto `ChatbotAPI/ChatbotAPI.csproj`.
- Serve para abrir e organizar a aplicacao inteira na IDE.

## `docker-compose.yml`
- Orquestra os containers do projeto.
- Define o servico `database` com SQL Server 2022.
- Define o servico `backend` que faz build da pasta `ChatbotAPI`.
- Define o servico `frontend` que faz build da pasta `chatbot-frontend`.
- Tambem configura portas e variaveis de ambiente para a conexao do backend com o banco.

## Backend ASP.NET Core

## `ChatbotAPI/ChatbotAPI.csproj`
- E o arquivo principal do projeto .NET.
- Define `net10.0` como framework alvo.
- Ativa `nullable` e `implicit usings`.
- Guarda o `UserSecretsId`, usado para configuracoes sensiveis.
- Lista dependencias importantes:
- `Microsoft.EntityFrameworkCore.SqlServer` para acesso ao SQL Server.
- `Microsoft.EntityFrameworkCore.Tools` e `Design` para migrations.
- `QuestPDF` para gerar PDF.
- `UglyToad.PdfPig` para ler arquivos PDF.

## `ChatbotAPI/Program.cs`
- E o ponto de entrada da API.
- Cria o `WebApplicationBuilder`.
- Registra controllers.
- Configura validacao automatica de modelos com `ApiBehaviorOptions`.
- Padroniza erro de validacao para o formato `{ erro: ... }`.
- Habilita OpenAPI em ambiente de desenvolvimento.
- Configura politica de CORS para aceitar o frontend.
- Le a `DefaultConnection` da configuracao.
- Registra o `AppDbContext` com SQL Server.
- Faz bind da secao `Gemini` para `GeminiOptions`.
- Registra servicos de aplicacao com injeccao de dependencia.
- Registra o `GeminiClient` com `AddHttpClient`.
- Insere o middleware global de excecao.
- Mapeia controllers e inicializa a aplicacao.

## `ChatbotAPI/appsettings.json`
- Arquivo principal de configuracao do backend.
- Normalmente contem `Logging`, `AllowedHosts`, `ConnectionStrings` e `Gemini`.
- Define a string de conexao principal do banco.
- Define `ApiKey`, `Model` e `BaseUrl` da integracao com Gemini.
- Em producao, o ideal e nao deixar segredos reais versionados aqui.

## `ChatbotAPI/appsettings.Development.json`
- Arquivo de configuracao complementar para ambiente de desenvolvimento.
- Permite sobrescrever parametros do `appsettings.json`.
- No momento esta bem enxuto e guarda apenas configuracao de log.

## `ChatbotAPI/appsettings copy.json`
- Arquivo de apoio usado como modelo local de configuracao.
- Ajuda a lembrar a estrutura esperada para banco e Gemini.
- Na pratica ele funciona mais como exemplo/manual do que como arquivo lido automaticamente pelo ASP.NET.

## `ChatbotAPI/ChatbotAPI.http`
- Arquivo de testes manuais de requisicao HTTP.
- Permite testar a API direto da IDE.
- Mostra um exemplo para criar sessao e outro para enviar mensagem.
- E util para demonstração rapida e debug.

## `ChatbotAPI/Dockerfile`
- Faz o empacotamento do backend em container.
- Usa imagem `dotnet/sdk` para build.
- Executa `dotnet restore` e `dotnet publish`.
- Depois copia a saida para imagem `dotnet/aspnet`.
- O container final sobe a API com `dotnet ChatbotAPI.dll`.

## `ChatbotAPI/Properties/launchSettings.json`
- Define perfis de execucao local pelo `dotnet run` e pela IDE.
- Configura portas HTTP e HTTPS para desenvolvimento.
- Define `ASPNETCORE_ENVIRONMENT=Development`.
- Facilita rodar localmente sem precisar informar porta manualmente.

## Controllers

## `ChatbotAPI/Controllers/ChatController.cs`
- E o controller principal da API.
- Expone as rotas relacionadas ao chat.
- Recebe requests e devolve responses tipados.
- Nao guarda a regra de negocio pesada; apenas delega para `IChatService`.
- Endpoints principais:
- `POST /api/chat/nova-sessao`
- `POST /api/chat/sessao/{id}/documento`
- `DELETE /api/chat/sessao/{id}/documento`
- `POST /api/chat/sessao/{id}/audio`
- `POST /api/chat/enviar-mensagem`
- `POST /api/chat/sessao/{id}/encerrar-e-exportar-resumo`

## Data e persistencia

## `ChatbotAPI/Data/AppDbContext.cs`
- E o contexto do Entity Framework Core.
- Representa a conexao logica entre a aplicacao e o banco.
- Expone `DbSet<ChatSession>` e `DbSet<ChatMessage>`.
- A partir dele o EF gera queries, migrations e mapeamento relacional.

## Models

## `ChatbotAPI/Models/ChatSession.cs`
- Representa uma sessao de conversa.
- Campos principais:
- `Id`: identificador da sessao.
- `SystemPrompt`: prompt base que orienta o comportamento da IA.
- `DocumentName`: nome do documento anexado.
- `DocumentContext`: texto extraido do documento.
- `CreatedAt`: data de criacao.
- `Messages`: lista de mensagens associadas.

## `ChatbotAPI/Models/ChatMessage.cs`
- Representa cada mensagem enviada ou recebida.
- Campos principais:
- `Id`
- `SessionId`
- `Role`: identifica se a mensagem e do usuario ou do assistente.
- `Content`: conteudo textual da mensagem.
- `CreatedAt`
- `Session`: navegacao para a sessao pai.

## Options

## `ChatbotAPI/Options/GeminiOptions.cs`
- Classe de configuracao da integracao com Gemini.
- Mapeia a secao `Gemini` do `appsettings`.
- Propriedades:
- `ApiKey`
- `Model`
- `BaseUrl`

## Middleware

## `ChatbotAPI/Middleware/ExceptionHandlingMiddleware.cs`
- Middleware global para tratamento de excecoes nao tratadas.
- Fica no pipeline HTTP antes dos controllers.
- Captura erros inesperados.
- Registra o erro com `ILogger`.
- Devolve uma resposta JSON padronizada com status 500.
- Evita que a API retorne stack trace cru para o cliente.

## Contracts de request e response

## `ChatbotAPI/Contracts/Requests/SendMessageRequest.cs`
- DTO usado para receber o endpoint de envio de mensagem.
- Campos:
- `SessionId`
- `Texto`
- Tambem tem validacao com `DataAnnotations`.
- Garante que `SessionId` seja maior que zero e que `Texto` nao venha vazio.

## `ChatbotAPI/Contracts/Responses/CreateSessionResponse.cs`
- DTO de resposta do endpoint de criacao de sessao.
- Devolve o `SessionId` criado no banco.

## `ChatbotAPI/Contracts/Responses/UploadDocumentResponse.cs`
- DTO de resposta do upload de documento.
- Devolve:
- `DocumentName`
- `ExtractedCharacters`
- `SuggestedQuestions`

## `ChatbotAPI/Contracts/Responses/RemoveDocumentResponse.cs`
- DTO de resposta da remocao de documento.
- Retorna o booleano `Removido`.

## `ChatbotAPI/Contracts/Responses/TranscribeAudioResponse.cs`
- DTO de resposta da transcricao de audio.
- Devolve o texto transcrito em `Transcript`.

## `ChatbotAPI/Contracts/Responses/SendMessageResponse.cs`
- DTO de resposta do endpoint principal de conversa.
- Devolve a resposta da IA em `Resposta`.

## `ChatbotAPI/Contracts/Responses/ErrorResponse.cs`
- DTO padrao de erro da API.
- Centraliza o formato `{ erro: "..." }`.
- E usado em validacao, falha de negocio e excecoes globais.

## Services

## `ChatbotAPI/Services/IChatService.cs`
- Interface principal da camada de negocio.
- Define os metodos usados pelo controller:
- criar sessao
- anexar documento
- transcrever audio
- remover documento
- enviar mensagem
- exportar resumo

## `ChatbotAPI/Services/ChatService.cs`
- E o servico central da aplicacao.
- Orquestra banco, documentos, cliente Gemini e geracao de PDF.
- Responsabilidades:
- criar sessoes
- salvar mensagens do usuario
- montar o prompt completo com contexto de documento
- chamar o `GeminiClient`
- salvar a resposta do assistente
- gerar perguntas sugeridas
- exportar resumo em PDF
- Tambem contem classes internas de resultado:
- `ChatResponse`
- `DocumentUploadResponse`
- `SummaryExportResponse`
- `AudioTranscriptionResponse`

## `ChatbotAPI/Services/IDocumentService.cs`
- Interface da camada dedicada a documentos.
- Define o metodo `ProcessDocumentsAsync`.
- Ajuda a separar upload e extracao de texto da logica principal do chat.

## `ChatbotAPI/Services/DocumentService.cs`
- Implementa a interface `IDocumentService`.
- Processa arquivos enviados pelo usuario.
- Valida quantidade, tamanho por arquivo e tamanho total do envio.
- Suporta arquivos `.txt` e `.pdf`.
- Usa `StreamReader` para texto plano.
- Usa `PdfPig` para extrair texto de PDF.
- Monta `DocumentContext` e `DocumentName` usados no chat.

## `ChatbotAPI/Services/DocumentProcessingResult.cs`
- Classe de retorno do `DocumentService`.
- Encapsula sucesso/falha do processamento.
- Devolve:
- `Success`
- `DocumentName`
- `DocumentContext`
- `ExtractedCharacters`
- `ErrorMessage`

## `ChatbotAPI/Services/SimplePdfWriter.cs`
- Classe utilitaria de geracao de PDF.
- Usa `QuestPDF`.
- Recebe titulo, subtitulo, markdown e rodape.
- Converte markdown simples em PDF.
- Entende titulos, subtitulos, bullets e paragrafos.
- E usada na funcionalidade de exportar resumo final da conversa.

## Clients

## `ChatbotAPI/Clients/IGeminiClient.cs`
- Interface do client de integracao com Gemini.
- Define operacoes de alto nivel:
- gerar resposta do chat
- gerar perguntas sugeridas
- gerar resumo
- transcrever audio

## `ChatbotAPI/Clients/GeminiClient.cs`
- Implementa `IGeminiClient`.
- Centraliza toda a comunicacao HTTP com a API Gemini.
- Usa `HttpClient` injetado.
- Monta payloads JSON para cada tipo de requisicao.
- Interpreta a resposta do Gemini e devolve `GeminiTextResult`.
- Concentra a logica de integracao externa em um unico lugar.

## `ChatbotAPI/Clients/GeminiChatMessage.cs`
- DTO interno usado para representar o historico enviado ao Gemini.
- Contem `Role` e `Content`.

## `ChatbotAPI/Clients/GeminiTextResult.cs`
- DTO de retorno das chamadas ao Gemini.
- Campos:
- `Success`
- `Text`
- `ErrorMessage`

## Migrations

## `ChatbotAPI/Migrations/20260310001733_BancoInicial.cs`
- Primeira migration do banco.
- Cria a tabela `ChatSessions`.
- Cria a tabela `ChatMessages`.
- Cria a chave estrangeira entre mensagem e sessao.

## `ChatbotAPI/Migrations/20260310001733_BancoInicial.Designer.cs`
- Arquivo gerado automaticamente pelo Entity Framework.
- Guarda o estado detalhado do modelo no momento da migration `BancoInicial`.
- Serve de apoio interno para comparacao de schema.

## `ChatbotAPI/Migrations/202605250001_AddDocumentContext.cs`
- Segunda migration relevante.
- Adiciona `DocumentContext` e `DocumentName` na tabela `ChatSessions`.
- Permite que a sessao armazene o documento usado como contexto.

## `ChatbotAPI/Migrations/202605250001_AddDocumentContext.Designer.cs`
- Arquivo gerado automaticamente pelo EF para a migration `AddDocumentContext`.
- Registra o modelo completo apos a adicao dos novos campos.

## `ChatbotAPI/Migrations/AppDbContextModelSnapshot.cs`
- Snapshot mais recente do modelo do banco.
- O EF usa esse arquivo para comparar o estado atual com futuras alteracoes.
- Pode ser entendido como a fotografia oficial do schema atual.

## Frontend React

## `chatbot-frontend/package.json`
- Define o projeto frontend, scripts e dependencias.
- Scripts principais:
- `npm run dev`
- `npm run build`
- `npm run preview`
- Dependencias importantes:
- `react`
- `react-dom`
- `react-markdown`
- `framer-motion`
- `@heroicons/react`

## `chatbot-frontend/package-lock.json`
- Arquivo de lock das dependencias do npm.
- Garante reproducao exata das versoes instaladas.
- Importante para consistencia entre maquinas.

## `chatbot-frontend/README.md`
- Documento introdutorio do frontend.
- Explica instalacao, variavel `VITE_API_BASE_URL`, desenvolvimento e build.

## `chatbot-frontend/.env.example`
- Exemplo de configuracao de ambiente do frontend.
- Mostra a variavel `VITE_API_BASE_URL`.
- Ajuda a apontar o frontend para a API correta.

## `chatbot-frontend/.gitignore`
- Define quais arquivos nao devem ser versionados no frontend.
- Ignora `node_modules`, `dist`, `.env`, logs e arquivos de editor.

## `chatbot-frontend/index.html`
- Documento HTML base servido pelo Vite.
- Contem o elemento `root`, onde o React monta a aplicacao.

## `chatbot-frontend/vite.config.ts`
- Configuracao do Vite.
- Ativa plugin React.
- Define a porta `5173`.
- Usa `host: true`, o que ajuda em execucao via rede e Docker.
- Usa `watch.usePolling` para melhorar atualizacao no Windows.

## `chatbot-frontend/tsconfig.json`
- Configuracao principal do TypeScript do frontend.
- Define alvo `ES2020`.
- Ativa modo estrito.
- Configura JSX para React.
- Usa `moduleResolution: bundler`.

## `chatbot-frontend/tsconfig.node.json`
- Configuracao complementar para arquivos do ambiente Node.
- E usada especialmente no `vite.config.ts`.

## `chatbot-frontend/tailwind.config.js`
- Configura o Tailwind CSS.
- Define os caminhos que serao analisados para classes utilitarias.
- Tambem ativa o plugin `@tailwindcss/typography`.

## `chatbot-frontend/postcss.config.js`
- Configura o PostCSS.
- Liga `tailwindcss` e `autoprefixer`.

## `chatbot-frontend/Dockerfile`
- Empacota o frontend em container.
- Usa `node:18`.
- Instala dependencias.
- Copia o codigo fonte.
- Roda `npm run dev -- --host`.

## Entrada e configuracao do frontend

## `chatbot-frontend/src/main.tsx`
- Ponto de entrada do React.
- Cria a raiz com `ReactDOM.createRoot`.
- Renderiza o componente `App`.
- Importa os estilos globais de `App.css`.

## `chatbot-frontend/src/App.css`
- Arquivo de estilos globais.
- Importa as diretivas `@tailwind`.
- Define `box-sizing`, altura total da tela e fonte base.

## `chatbot-frontend/src/config/api.ts`
- Centraliza a URL base da API.
- Usa `VITE_API_BASE_URL` se existir.
- Caso contrario, usa `http://localhost:5283/api`.

## `chatbot-frontend/src/types/chat.ts`
- Define os tipos principais usados no frontend.
- `ChatSession`
- `ChatMessage`
- `ChatLocalStorage`
- `MessageRequest`
- `ApiResponse`
- Ajuda o projeto a manter consistencia tipada.

## `chatbot-frontend/src/utils/chatCache.ts`
- Gerencia persistencia local com `localStorage`.
- Guarda sessoes, mensagens e sessao ativa.
- Cria IDs locais com `randomUUID` quando possivel.
- Faz saneamento de dados antigos ou invalidos.
- Tambem define um TTL para expirar cache.

## Hook de integracao com a API

## `chatbot-frontend/src/hooks/useChatAPI.ts`
- Hook central de comunicacao com o backend.
- Expone funcoes:
- `criarSessao`
- `anexarDocumento`
- `removerDocumento`
- `enviarMensagem`
- `transcreverAudio`
- `exportarResumo`
- Mantem estados `loading` e `error`.
- Tambem padroniza leitura de mensagens de erro da API.

## Componente raiz do frontend

## `chatbot-frontend/src/App.tsx`
- E o componente principal da interface.
- Mantem o estado das sessoes, mensagens e sessao ativa.
- Usa `loadPersistedChatState` e `savePersistedChatState`.
- Exibe sugestoes de tipos de sessao.
- Controla sidebar, modal e area principal do chat.
- Integra `SessionList`, `ChatWindow` e `NewSessionModal`.

## Componentes de interface

## `chatbot-frontend/src/components/ChatWindow.tsx`
- E o componente central da experiencia de conversa.
- Recebe as mensagens da sessao atual.
- Usa o hook `useChatAPI`.
- Envia mensagem, anexa documentos, remove documento, transcreve audio e exporta resumo.
- Controla animacao de digitacao da resposta do assistente.
- Exibe perguntas sugeridas quando existem.

## `chatbot-frontend/src/components/InputArea.tsx`
- E a area de entrada do usuario.
- Permite:
- digitar mensagem
- anexar documento
- enviar audio de arquivo
- gravar audio no navegador
- remover documento atual
- Trabalha com `MediaRecorder` para captacao de audio.

## `chatbot-frontend/src/components/MessageList.tsx`
- Renderiza a lista de mensagens da conversa.
- Faz auto-scroll ate a ultima mensagem.
- Exibe o documento ativo.
- Exibe indicador de digitacao do bot.

## `chatbot-frontend/src/components/MessageItem.tsx`
- Renderiza uma unica mensagem.
- Diferencia visualmente usuario e assistente.
- Usa `react-markdown` para respostas do bot.
- Permite copiar a mensagem do assistente com um botao.

## `chatbot-frontend/src/components/SessionList.tsx`
- Representa a barra lateral de sessoes.
- Lista todas as conversas criadas.
- Permite criar nova sessao.
- Permite abrir modal de confirmacao para excluir conversa.

## `chatbot-frontend/src/components/SessionItem.tsx`
- Representa um item individual da lista de sessoes.
- Mostra uma versao truncada do system prompt.
- Mostra a data da sessao.
- Mostra o documento vinculado, quando existir.

## `chatbot-frontend/src/components/NewSessionModal.tsx`
- Modal para criacao manual de nova sessao.
- Permite ao usuario escrever um `systemPrompt`.
- Envia esse prompt para a criacao da sessao.

## `chatbot-frontend/src/components/Modal.tsx`
- Componente base reutilizavel de modal.
- Usa `framer-motion` para animacao de entrada e saida.
- E usado no frontend para confirmacoes.

## Fluxo tecnico de uma mensagem
- O usuario escreve no `InputArea`.
- O `ChatWindow` adiciona a mensagem localmente.
- O hook `useChatAPI` chama `POST /api/chat/enviar-mensagem`.
- O `ChatController` recebe o request.
- O `ChatService` salva a mensagem, le o historico e monta o prompt.
- O `GeminiClient` envia a conversa para o Gemini.
- O retorno da IA e salvo no banco.
- O frontend exibe a resposta com animacao de digitacao.

## Fluxo tecnico de documento
- O usuario escolhe PDF ou TXT.
- O `InputArea` envia os arquivos ao `ChatWindow`.
- O `ChatWindow` chama `useChatAPI.anexarDocumento`.
- O `ChatController` delega ao `ChatService`.
- O `ChatService` chama `DocumentService`.
- O `DocumentService` extrai texto, monta `DocumentContext` e devolve resultado.
- O `ChatService` salva o contexto na sessao e pode pedir perguntas sugeridas ao Gemini.

## Fluxo tecnico de audio
- O usuario pode gravar ou anexar audio.
- O frontend envia o arquivo para `/api/chat/sessao/{id}/audio`.
- O `ChatService` valida tamanho e extensao.
- O audio e convertido para Base64.
- O `GeminiClient` chama o Gemini para transcricao.
- O texto transcrito volta para o frontend e pode ser enviado como mensagem.

## Fluxo tecnico de resumo em PDF
- O usuario aciona `Exportar resumo`.
- O frontend chama o endpoint `encerrar-e-exportar-resumo`.
- O `ChatService` busca a sessao e o historico.
- O `GeminiClient` tenta gerar um resumo em markdown.
- Se falhar, o `ChatService` usa um resumo de fallback.
- O `SimplePdfWriter` transforma o markdown em PDF.
- O frontend recebe o arquivo e baixa automaticamente.

## O que nao precisa ser explicado em detalhe na aula
- `bin/`: resultado de compilacao do backend.
- `obj/`: arquivos intermediarios do build do .NET.
- `node_modules/`: dependencias instaladas do frontend.
- Esses itens existem para a aplicacao rodar, mas nao representam codigo de autoria principal do projeto.

## Sugestao de ordem para sua apresentacao
- Primeiro explique o objetivo geral do sistema.
- Depois mostre a divisao entre frontend, backend e banco.
- Em seguida explique o backend: `Program`, `Controller`, `Services`, `Clients`, `Data`, `Models`, `Contracts`.
- Depois explique o frontend: `App`, hook da API, cache local e componentes principais.
- Feche com os fluxos mais importantes: mensagem, documento, audio e resumo.

## Resumo final
- O backend organiza regras de negocio, persistencia, integracao com IA e geracao de PDF.
- O frontend organiza sessao, historico local, interface de conversa e consumo da API.
- O banco guarda sessoes e mensagens.
- O Gemini fornece inteligencia generativa, transcricao e sumarizacao.
- O projeto como um todo forma uma aplicacao web completa, com arquitetura separada em camadas e integracao entre frontend, backend, banco e IA.
