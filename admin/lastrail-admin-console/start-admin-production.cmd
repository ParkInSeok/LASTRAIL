@echo off
set LASTRAIL_ADMIN_PROFILE=production
set ADMIN_BACKEND_HOST=127.0.0.1
set ADMIN_BACKEND_PORT=4174
npm --prefix lastrail-admin-console\apps\admin-backend run dev
