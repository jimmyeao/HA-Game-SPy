# HA-Game-SPy

[![CodeQL](https://github.com/jimmyeao/HA-Game-SPy/actions/workflows/codeql.yml/badge.svg)](https://github.com/jimmyeao/HA-Game-SPy/actions/workflows/codeq.yml)[![Codespaces Prebuilds](https://github.com/jimmyeao/HA-Game-SPy/actions/workflows/codespaces/create_codespaces_prebuilds/badge.svg)](https://github.com/jimmyeao/HA-Game-SPy/actions/workflows/codespaces/create_codespaces_prebuilds)

The purpose of this windows application is watch for certain Games by looking for the windows process. You could use it for any windows process..
This will create a sensor in MQTT that should be autodiscovered by Homeassistant (At the moment, you will need to be using MQTT, the direct HomeAssistant integration hasnt been written yet!)
The sensor follows the form sensor.hagamespy_yourcomputername e.g.:
sensor.hagamespy_ryzen

You can use a Markdown card to show the status
```type: markdown
content: |
  ![Game Image]({{ state_attr('sensor.hagamespy_ryzen', 'gamelogourl') }})
  **Device ID:** {{ state_attr('sensor.hagamespy_ryzen', 'device_id') }}
  **Game Name:** {{ states('sensor.hagamespy_ryzen') }}
```
And the card will appear something like this:

![image](https://github.com/jimmyeao/HA-Game-SPy/assets/5197831/caa6e8c2-de9c-4e02-8ff2-b3ad3c132a5f)

from this, you can then build out automations to do things like set a lighting scene, or even notify you if the kids are up late gaming etc!
The application can be run minimised and at boot, there is a very small list of games included, but I have added functionality in to allow you to add games at will.
If anyone has a good list of game names and their exe names, please creata PR for the games.json file, the more the better :)

Main Window:

<img src="https://github.com/jimmyeao/HA-Game-SPy/assets/5197831/ec483760-9159-4346-a8c6-b7ad944b37fe" width="600" >


Add Game:

<img src="https://github.com/jimmyeao/HA-Game-SPy/assets/5197831/b3d2c25e-0fdb-4cde-ad1f-5e3b81cd09e3" width="600" >

You can use the browse button to find the executable for your game, fill in the name, and the image url can either be a source on the internet, or hosted locally on your homeassistant (Assuming you have an image in /config/www/images/ your url would be /local/images/yourimage.png) NOTE if you publish a local (to Home assistant) url, the banner image currently wont display in the app, only in Home Assistant. I may fix this in the future :)

List Games:

<img src="https://github.com/jimmyeao/HA-Game-SPy/assets/5197831/513a972c-b9dd-4a09-8ad1-0b657ee80283" width="600" >

you can edit and remove/add games in the list view too




