<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/othneildrew/Best-README-Template">
    <img src="images/logo.png" alt="Logo" width="80" height="80">
  </a>

  <h3 align="center">QSSH</h3>

  <p align="center">
    Utilidad para facilitar la automatización de comandos enviados a través de SSH hacía Linux.
    <br />
    <a href="https://github.com/Redyenx/QSSH/tree/master/Docs"><strong>Explorar la documentación »</strong></a>
    <br />
    <br />
    ·
    <a href="https://github.com/Redyenx/QSSH/issues">Reportar Bug</a>
    ·
    <a href="https://github.com/Redyenx/QSSH/issues">Solicitar función</a>
  </p>
</p>



<!-- TABLE OF CONTENTS -->
<details open="open">
  <summary>Tabla de contenidos</summary>
  <ol>
    <li>
      <a href="#sobre-el-proyecto">Sobre el proyecto</a>
      <ul>
        <li><a href="#desarrollado-con">Desarrollado con</a></li>
      </ul>
    </li>
    <li>
      <a href="#primeros-pasos">Primeros pasos</a>
      <ul>
        <li><a href="#prerrequisitos">Prerrequisitos</a></li>
        <li><a href="#instalación">Instalación</a></li>
      </ul>
    </li>
    <li><a href="#uso">Uso</a></li>
    <li><a href="#hoja-de-ruta">Hoja de ruta</a></li>
    <li><a href="#contacto">Contacto</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## Sobre el proyecto

![Qssh Screen Shot][qssh-screenshot]


Qssh, es una pequeña utilidad desarrollada para facilitar la implementación de procesos y servicios de automatización  mediante el protocolo SSH, simplificando así su alcance y su implementación a través de diversas plataformas de automatización.


### Desarrollado con

This section should list any major frameworks that you built your project using. Leave any add-ons/plugins for the acknowledgements section. Here are a few examples.
* [C#](https://visualstudio.microsoft.com/es/)
* [CommandLineParse](https://github.com/commandlineparser/commandline)
* [SSH.Net](https://github.com/sshnet/SSH.NET/)



<!-- GETTING STARTED -->
## Primeros pasos

This is an example of how you may give instructions on setting up your project locally.
To get a local copy up and running follow these simple example steps.

### Prerrequisitos

QSSH admite los siguientes versiones de .Net Framework:
  ```sh
  .NET Framework 4.5 o superior
  ```

### Instalación

1. Descargue **"qssh.exe"** de la carpeta "install" de este repositorio.
2. Ubíquelo en un directorio y acceda a este a través de la línea de comando de Windows.

![Qssh Demo][qssh-demo]

<!-- USAGE EXAMPLES -->
## Uso
**Crear perfil de conexión:** se pueden administrar varios perfiles de conexión, esto con el fin de abreviar la sintaxis de comandos y facilitar su mantenimiento, para crear y/o actualizar se utilizará el siguiente comando:
```sh
qssh.exe --logging --user "nombre_usuario" --password "contraseña" --host "IP" --port "puerto" –-profile "nombre_perfil" --save
qssh.exe --logging --user osboxes --password osboxes.org --host 192.168.1.188 --port 2222 --profile Server1 --save
```
**Ver perfiles de conexión:** Muestra los perfiles de conexión almacenados.
```sh
qssh.exe --load "perfil" --view
qssh.exe --load Server1 --view
```
Puede usar **–view** en cualquier comando para visualizar los datos del perfil utilizado para la conexión.

**Enviar comandos:** Envío de comandos simples sin permisos de elevación.
```sh
qssh.exe --command [comandos] --load "nombre_perfil"
qssh.exe --command "pwd" "whoami" --load Server1 
```
**Enviar comandos sin perfiles de conexión:**
```sh
qssh.exe --logging --user "nombre_usuario" --password "contraseña" --host "IP" --port "puerto"
 --command "comando"
qssh.exe --logging --user osboxes --password osboxes.org --host 192.168.1.107 --port 2222 
--command "pwd"
```

**Nota:** Se pueden enviar varias secuencias de comandos en un solo envío, se recomienda que, por cada secuencia, se encierre entre comillas “”.

**Guardar registro:** Habilita el guardado del registro (De momento se almacena junto al programa ejecutable).
```sh
qssh.exe --command "pwd" "whoami" --load Server1 –log
```
**Comandos con elevación:** Los comandos enviados aquí, se envían si requieren elevación, este ingresará la contraseña suministrada. 
```sh
qssh.exe --sudo [comandos] --load "nombre_perfil" --log
qssh.exe --sudo [comandos] --load Server1 --log
```
El siguiente comando detiene un servicio, lo vuelve a iniciar, muestra su estado y almacena la salida en un registro.
```sh
qssh.exe qssh.exe --sudo "systemctl stop smartd" "systemctl start smartd" "systemctl is-active smartd" --load Server1 --log
```
**Nota:** Se pueden enviar varias secuencias de comandos en un solo envío, se recomienda que, por cada secuencia, se encierre entre comillas “”.

**Administración de servicios:** mediante esta utilidad se pueden administrar servicios y retornar una respuesta.
El siguiente ejemplo detiene un servicio.
``` sh
qssh.exe --service "servicio" --status 0 --load Server1
```
Para enviar un estado (Iniciar, Detener, Reiniciar) cuenta con las siguientes opciones:

--status 0 = Detener
--status 1 = Iniciar
--status 2 = Reiniciar
--status 3 = Ver estado actual del servicio

**Ejemplo:**
Detener/Iniciar/Estado
```sh
  qssh.exe --service "smartd" --status 0 --load Server1
  qssh.exe --service "smartd" --status 1 --load Server1
  qssh.exe --service "smartd" --status 3 --load Server1
```

**Envío de archivos:** Esta función permite el envío de archivos, útil para enviar scripts, que posteriormente pueden ser invocados a través de comandos (--command) o con permisos de ejecución elevados con sudo (--sudo). De igual forma permite múltiple envío de archivos siempre y cuando estén separados por comillas “”.
```sh
qssh.exe --upload [archivos] --sudo [comandos] --load "nombre_perfil"
qssh.exe --upload "C:\Scripts\TestA.sh" "C:\Scripts\TestB.sh" "C:\Scripts\TestC.sh" --sudo "bash /home/osboxes/TestA.sh" "bash /home/osboxes/testB.sh" --load Server1
```

**Envío de comandos a través de archivo de texto: (BETA)** si ubica un archivo de texto adjunto a qssh.exe, que por el momento se almacena con el nombre **"bash.txt"**, dentro de este puede ubicar cadenas de texto y listas de cadenas de texto para enviar múltiples parámetros por conexión o una por conexión que soporta parámetros. Esta función no se encuentra finalizada.
```sh
qssh.exe --script --load Server1
qssh.exe --script --load Server1
```

<!-- ROADMAP -->
## Hoja de ruta

Consulte los [temas pendientes](https://github.com/Redyenx/QSSH/issues) para obtener una lista de funciones propuestas (y problemas conocidos).

<!-- CONTACT -->
## Contacto

Sneyder Sarmiento - essarmiento@quantic.com.co

Project Link: https://github.com/Redyenx/QSSH](https://github.com/Redyenx/QSSH)

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[qssh-screenshot]: images/screenshot.png
[qssh-demo]: images/demo.gif
