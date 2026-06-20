import axios from 'axios';

const apiClient = axios.create({
baseURL: 'http://localhost:5123',
});

export default apiClient;