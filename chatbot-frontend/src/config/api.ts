// Configurar a URL base da API baseado no ambiente
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5283/api';

export { API_BASE_URL };
