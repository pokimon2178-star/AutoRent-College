#!/bin/bash
# Простой тест функциональности аутентификации

echo "=== Простой тест аутентификации ==="
echo ""

cd "C:\Users\novo99\AI-Agent-Workspace\AutoRent" 2>/dev/null || cd .

# Запуск приложения
echo "1. Запуск приложения..."
dotnet run > /dev/null 2>&1 &
APP_PID=$!
sleep 5

echo "2. Проверка страниц..."
echo -n "   - Главная страница: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5016
echo ""

echo -n "   - Регистрация: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5016/Account/Register
echo ""

echo -n "   - Вход: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5016/Account/Login
echo ""

echo -n "   - Доступ запрещён: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5016/Account/AccessDenied
echo ""

echo -n "   - Каталог автомобилей: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5016/Cars
echo ""

echo ""
echo "3. Проверка навигации для неавторизованных..."
NAVIGATION=$(curl -s http://localhost:5016 | grep -E "(Войти|Регистрация)" | wc -l)
if [ "$NAVIGATION" -ge "2" ]; then
    echo "   ✓ Навигация отображается ($NAVIGATION ссылок)"
else
    echo "   ✗ Навигация не найдена"
fi

echo ""
echo "4. Проверка защиты форм..."
REGISTER_TOKEN=$(curl -s http://localhost:5016/Account/Register | grep -c "__RequestVerificationToken")
LOGIN_TOKEN=$(curl -s http://localhost:5016/Account/Login | grep -c "__RequestVerificationToken")
echo "   - Anti-Forgery на регистрации: $REGISTER_TOKEN токен(ов)"
echo "   - Anti-Forgery на входе: $LOGIN_TOKEN токен(ов)"

echo ""
echo "5. Проверка валидации..."
VALIDATIONS=$(curl -s http://localhost:5016/Account/Register | grep -c "data-val")
echo "   - Атрибуты валидации: $VALIDATIONS"

echo ""
echo "6. Проверка каталога..."
CARS=$(curl -s http://localhost:5016/Cars | grep -c "card-title")
echo "   - Автомобилей в каталоге: $CARS"

echo ""
echo "Завершение..."
kill $APP_PID 2>/dev/null
wait $APP_PID 2>/dev/null

echo ""
echo "=== Тест завершён ==="
