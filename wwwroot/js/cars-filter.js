// AJAX-фильтрация каталога автомобилей
(function () {
    const filterForm = document.getElementById('filterForm');
    const carsContainer = document.getElementById('carsContainer');
    const resetButton = document.getElementById('resetFilters');

    if (!filterForm || !carsContainer) {
        return;
    }

    // Обработка отправки формы
    filterForm.addEventListener('submit', function (e) {
        e.preventDefault();
        applyFilters();
    });

    // Обработка сброса фильтров
    if (resetButton) {
        resetButton.addEventListener('click', function () {
            filterForm.reset();
            applyFilters();
        });
    }

    // Применение фильтров с помощью AJAX
    function applyFilters() {
        // Показать индикатор загрузки
        carsContainer.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Загрузка...</span></div></div>';

        // Собрать данные формы
        const formData = new FormData(filterForm);
        const params = new URLSearchParams();

        // Добавить только непустые значения
        for (const [key, value] of formData.entries()) {
            if (value && value.trim() !== '') {
                params.append(key, value);
            }
        }

        // Отправить AJAX-запрос
        fetch('/Cars/FilterPartial?' + params.toString(), {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.text();
            })
            .then(html => {
                // Обновить контейнер с автомобилями
                carsContainer.innerHTML = html;

                // Обновить URL без перезагрузки страницы
                const newUrl = '/Cars?' + params.toString();
                window.history.pushState({ path: newUrl }, '', newUrl);
            })
            .catch(error => {
                console.error('Error:', error);
                carsContainer.innerHTML = `
                    <div class="alert alert-danger" role="alert">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        <strong>Ошибка загрузки данных</strong>
                        <p class="mb-0 mt-2">Попробуйте обновить страницу или изменить параметры фильтрации</p>
                    </div>
                `;
            });
    }

    // Обработка кнопки "Назад" в браузере
    window.addEventListener('popstate', function () {
        location.reload();
    });
})();
