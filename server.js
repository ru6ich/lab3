// const express = require('express');
// const mysql = require('mysql2');
// const cors = require('cors');

// const app = express();
// const PORT = 3000;

// // Настройки для соединения с базой данных
// const db = mysql.createConnection({
//     host: 'localhost',
//     user: 'root', // Укажите вашего пользователя MySQL
//     password: '271202', // Укажите пароль, если есть
//     database: 'my_database'
// });

// // Подключение к БД
// db.connect(err => {
//     if (err) {
//         console.error('Ошибка подключения к БД:', err);
//         return;
//     }
//     console.log('Подключено к базе данных');
// });

// app.use(cors());
// app.use(express.json());

// // Маршрут для добавления новой темы
// app.post('/add-topic', (req, res) => {
//     const { topic_name, section_name, description, author_name, email, complexity, reading_time } = req.body;

//     if (!topic_name || !section_name || !description || !author_name || !email || !complexity || !reading_time) {
//         return res.status(400).json({ message: 'Все поля обязательны' });
//     }

//     // Проверяем, существует ли указанный раздел
//     db.query('SELECT section_id FROM sections WHERE section_name = ?', [section_name], (err, results) => {
//         if (err) {
//             console.error('Ошибка запроса:', err);
//             return res.status(500).json({ message: 'Ошибка сервера' });
//         }

//         let section_id;
//         if (results.length > 0) {
//             section_id = results[0].section_id;
//         } else {
//             // Если раздел не найден, создаем его
//             db.query('INSERT INTO sections (section_name) VALUES (?)', [section_name], (err, insertResult) => {
//                 if (err) {
//                     console.error('Ошибка добавления раздела:', err);
//                     return res.status(500).json({ message: 'Ошибка при создании раздела' });
//                 }
//                 section_id = insertResult.insertId;
//                 insertTopic(section_id);
//             });
//             return;
//         }
//         insertTopic(section_id);
//     });

//     function insertTopic(section_id) {
//         db.query('INSERT INTO topics (section_id, topic_name, description, difficulty, time_limit, author_name, author_email) VALUES (?, ?, ?, ?, ?, ?, ?)', 
//             [section_id, topic_name, description, complexity, reading_time, author_name, email], 
//             (err, result) => {
//                 if (err) {
//                     console.error('Ошибка добавления темы:', err);
//                     return res.status(500).json({ message: 'Ошибка при добавлении темы' });
//                 }
//                 res.status(201).json({ message: 'Тема успешно добавлена' });
//             }
//         );
//     }
// });

// app.listen(PORT, () => {
//     console.log(`Сервер запущен на порту ${PORT}`);
// });
