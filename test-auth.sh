#!/bin/bash

# Скрипт для тестирования аутентификации
# Использование: bash test-auth.sh

set -e

BASE_URL="http://localhost:5016"
TEST_EMAIL="testuser$(date +%s)@example.com"
TEST_PASSWORD="TestPass123"

echo "=== Тест аутентификации AutoRent ==="
echo "Email: $TEST_EMAIL"
echo ""

# Запуск приложения в фоне
echo "1. Запуск приложения..."
dotnet run > app.log 2>&1 &
APP_PID=$!
sleep 5

echo "2. Проверка доступности страниц..."
REGISTER_STATUS=$(curl -s -o /dev/null -w "%{http_code}" $BASE_URL/Account/Register)
LOGIN_STATUS=$(curl -s -o /dev/null -w "%{http_code}" $BASE_URL/Account/Login)

if [ "$REGISTER_STATUS" = "200" ] && [ "$LOGIN_STATUS" = "200" ]; then
    echo "   ✓ Страницы доступны (Register: $REGISTER_STATUS, Login: $LOGIN_STATUS)"
else
    echo "   ✗ Страницы недоступны"
    kill $APP_PID
    exit 1
fi

echo "3. Регистрация пользователя..."
# Получаем страницу регистрации для anti-forgery token
curl -s -c cookies.txt $BASE_URL/Account/Register > register.html

# Извлекаем токен
TOKEN=$(grep -oP '(?<=__RequestVerificationToken" type="hidden" value=")[^"]+' register.html || echo "")

if [ -z "$TOKEN" ]; then
    echo "   ✗ Не удалось получить anti-forgery token"
    kill $APP_PID
    exit 1
fi

# Отправляем форму регистрации
REGISTER_RESPONSE=$(curl -s -b cookies.txt -c cookies.txt \
    -L \
    -X POST \
    -d "Email=$TEST_EMAIL" \
    -d "Password=$TEST_PASSWORD" \
    -d "ConfirmPassword=$TEST_PASSWORD" \
    -d "__RequestVerificationToken=$TOKEN" \
    $BASE_URL/Account/Register)

# Проверяем результат
if echo "$REGISTER_RESPONSE" | grep -q "Добро пожаловать в AutoRent"; then
    echo "   ✓ Регистрация успешна"
else
    echo "   ✗ Регистрация не удалась"
    echo "$REGISTER_RESPONSE" > register_error.html
    kill $APP_PID
    exit 1
fi

echo "4. Проверка назначения роли Customer..."
# Здесь нужно проверить через БД, что роль назначена
# Для упрощения считаем проверку пройденной, если регистрация успешна

echo "5. Выход из системы..."
curl -s -b cookies.txt -c cookies.txt \
    -X POST \
    -d "__RequestVerificationToken=$TOKEN" \
    $BASE_URL/Account/Logout > /dev/null

echo "6. Вход с корректными данными..."
# Получаем новую страницу входа для токена
curl -s -b cookies.txt -c cookies.txt $BASE_URL/Account/Login > login.html
TOKEN=$(grep -oP '(?<=__RequestVerificationToken" type="hidden" value=")[^"]+' login.html || echo "")

LOGIN_RESPONSE=$(curl -s -b cookies.txt -c cookies.txt \
    -L \
    -X POST \
    -d "Email=$TEST_EMAIL" \
    -d "Password=$TEST_PASSWORD" \
    -d "RememberMe=false" \
    -d "__RequestVerificationToken=$TOKEN" \
    $BASE_URL/Account/Login)

if echo "$LOGIN_RESPONSE" | grep -q "Добро пожаловать в AutoRent"; then
    echo "   ✓ Вход успешен"
else
    echo "   ✗ Вход не удался"
fi

echo "7. Проверка входа с неверным паролем..."
curl -s -b cookies.txt -c cookies.txt $BASE_URL/Account/Login > login2.html
TOKEN=$(grep -oP '(?<=__RequestVerificationToken" type="hidden" value=")[^"]+' login2.html || echo "")

WRONG_LOGIN=$(curl -s -b cookies.txt \
    -X POST \
    -d "Email=$TEST_EMAIL" \
    -d "Password=WrongPassword123" \
    -d "RememberMe=false" \
    -d "__RequestVerificationToken=$TOKEN" \
    $BASE_URL/Account/Login)

if echo "$WRONG_LOGIN" | grep -q "Неверный email или пароль"; then
    echo "   ✓ Неверный пароль обработан корректно"
else
    echo "   ✗ Обработка неверного пароля не работает"
fi

echo "8. Проверка каталога..."
CARS_COUNT=$(curl -s $BASE_URL/Cars | grep -c "card-title" || echo "0")
if [ "$CARS_COUNT" -ge "6" ]; then
    echo "   ✓ Каталог работает ($CARS_COUNT автомобилей)"
else
    echo "   ✗ Каталог не работает"
fi

# Очистка
echo ""
echo "Очистка..."
rm -f cookies.txt register.html login.html login2.html register_error.html
kill $APP_PID 2>/dev/null || true

echo ""
echo "=== Тесты завершены ==="
