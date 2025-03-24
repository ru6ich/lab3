$(document).ready(function() {
    console.log('Документ готов, инициализация обработчиков...');
    
    // Проверяем наличие элементов на странице
    console.log('Количество разделов:', $('.section').length);
    console.log('Количество контейнеров тем:', $('.topics-container').length);
    console.log('Количество контейнеров контента:', $('.topics-content').length);
    
    // Обработчик клика по разделу
    $('.section').on('click', function() {
        console.log('Клик по разделу');
        const sectionId = $(this).find('h2').data('section-id');
        const container = $(this);
        const topicsContent = container.find('.topics-content');
        
        console.log('ID раздела:', sectionId);
        console.log('Контейнер:', container);
        console.log('Контейнер контента:', topicsContent);
        
        loadTopics(sectionId, container, topicsContent);
    });

    // Обработчик клика по кнопке редактирования (делегированный)
    $(document).on('click', '.edit-topic', function(e) {
        e.stopPropagation(); // Предотвращаем всплытие события
        console.log('Клик по кнопке редактирования');
        const topicId = $(this).data('topic-id');
        console.log('ID темы:', topicId);
        editTopic(topicId);
    });
});

// Функция для загрузки тем раздела
function loadTopics(sectionId, container, topicsContent) {
    console.log('Загрузка тем для раздела:', sectionId);
    
    const loadingIndicator = container.find('.loading-indicator');
    loadingIndicator.show();
    topicsContent.empty();
    
    // Исправляем URL для получения тем раздела
    $.get(`/api/topics/section/${sectionId}`)
        .done(function(topics) {
            console.log('Получены темы:', topics);
            loadingIndicator.hide();
            
            if (!topics || topics.length === 0) {
                console.log('Нет тем для отображения');
                topicsContent.html('<p>В этом разделе пока нет тем</p>');
                return;
            }
            
            topics.forEach(topic => {
                console.log('Создание карточки для темы:', topic);
                const topicHtml = `
                    <div class="topic-card" data-topic-id="${topic.id}">
                        <h3>${topic.topicName}</h3>
                        <p>${topic.description}</p>
                        <div class="topic-meta">
                            <span class="difficulty">Сложность: ${topic.difficulty} звезд</span>
                            <span class="time">Время: ${topic.timeLimit} минут</span>
                        </div>
                        <div class="topic-actions" style="margin-top: 10px;">
                            <button type="button" class="btn btn-primary edit-topic" data-topic-id="${topic.id}" style="display: inline-block;">
                                Редактировать
                            </button>
                        </div>
                    </div>
                `;
                console.log('Добавление HTML карточки в контейнер');
                topicsContent.append(topicHtml);
                console.log('HTML карточки добавлен');
            });
            
            // Проверяем, что кнопки добавлены
            console.log('Количество кнопок редактирования:', topicsContent.find('.edit-topic').length);
        })
        .fail(function(error) {
            console.error('Ошибка при загрузке тем:', error);
            loadingIndicator.hide();
            topicsContent.html('<div class="error-message">Ошибка при загрузке тем</div>');
        });
}

// Функция для редактирования темы
function editTopic(topicId) {
    // Получаем данные темы
    $.get('/api/topics/' + topicId)
        .done(function(topic) {
            // Создаем форму редактирования
            const formHtml = `
                <div class="edit-form">
                    <h3>Редактирование темы</h3>
                    <form id="editTopicForm">
                        <input type="hidden" name="id" value="${topic.id}">
                        <div class="form-group">
                            <label for="topicName">Название темы</label>
                            <input type="text" class="form-control" id="topicName" name="topicName" value="${topic.topicName}" required>
                        </div>
                        <div class="form-group">
                            <label for="description">Описание</label>
                            <textarea class="form-control" id="description" name="description" rows="3">${topic.description || ''}</textarea>
                        </div>
                        <div class="form-actions">
                            <button type="submit" class="btn btn-primary">Сохранить</button>
                            <button type="button" class="btn btn-secondary" onclick="closeEditForm()">Отмена</button>
                        </div>
                    </form>
                </div>
            `;

            // Добавляем форму на страницу
            $('body').append(formHtml);

            // Обработчик отправки формы
            $('#editTopicForm').submit(function(e) {
                e.preventDefault();
                const formData = {
                    id: topic.id,
                    topicName: $('#topicName').val(),
                    description: $('#description').val(),
                    difficulty: topic.difficulty,
                    timeLimit: topic.timeLimit,
                    sectionId: topic.sectionId
                };

                // Отправляем данные на сервер
                $.ajax({
                    url: '/api/topics/' + topicId,
                    method: 'PUT',
                    contentType: 'application/json',
                    data: JSON.stringify(formData)
                })
                .done(function(response) {
                    // Обновляем отображение темы на странице
                    const topicCard = $(`.topic-card[data-topic-id="${topicId}"]`);
                    topicCard.find('h3').text(formData.topicName);
                    topicCard.find('p').text(formData.description);
                    
                    // Закрываем форму
                    closeEditForm();
                })
                .fail(function(error) {
                    alert('Ошибка при сохранении темы');
                });
            });
        })
        .fail(function(error) {
            alert('Ошибка при загрузке данных темы');
        });
}

// Функция для закрытия формы редактирования
function closeEditForm() {
    $('.edit-form').remove();
} 