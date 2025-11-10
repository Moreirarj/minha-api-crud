-- 1. Primeiro delete todos os usuários
DELETE FROM Users;

-- 2. Depois reset a sequência  
DELETE FROM sqlite_sequence WHERE name='Users';

-- 3. Verifique se resetou
SELECT * FROM sqlite_sequence;