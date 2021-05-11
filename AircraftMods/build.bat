@echo off

call buildone zz-DA62X-mobi DA62X 0.1.0
call buildone z-DA62Asobo-mobi DA62Asobo 0.1.0

call copyone zz-DA62X-mobi DA62X 0.1.0

echo.
echo.
pause
