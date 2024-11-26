# HyperTension Bot

## Introduction

The HyperTension Bot is a Telegram bot that helps the user to keep track of his blood pressure and heart rate.
It also provides the user with general medical information about hypertension and how to measure blood pressure.
Instead, it will not answer questions not related to hypertension or general health.

> This project is provided under the Apache 2.0 license. See the [LICENSE](LICENSE) file for more information.

> Contributors: see the file [CONTRIBUTING.md](CONTRIBUTING.md).

> Contributors (code only): see the [github contributors page](https://github.com/MatteoMagnini/hyperTensionBot/graphs/contributors)

### Reference paper

> Sara Montagna, Gianluca Aguzzi, Stefano Ferretti, Martino Francesco Pengo, Lorenz Cuno Klopfenstein, Michelangelo Ungolo, Matteo Magnini. "LLM-based Solutions for Healthcare Chatbots: a Comparative Analysis", in: Proceedings of 2024 IEEE International Conference on Pervasive Computing and Communications Workshops and other Affiliated Events (PerCom Workshops).

Bibtex:
```bibtex
@inproceedings{llmbasedhealthcarechatbots-telmed2024,
  author={Montagna, Sara and Aguzzi, Gianluca and Ferretti, Stefano and Pengo, Martino Francesco and Klopfenstein, Lorenz Cuno and Ungolo, Michelangelo and Magnini, Matteo},
  booktitle={2024 IEEE International Conference on Pervasive Computing and Communications Workshops and other Affiliated Events (PerCom Workshops)}, 
  keywords = {large language model, medical chatbot, chronic disease management},
  title={LLM-based Solutions for Healthcare Chatbots: a Comparative Analysis}, 
  year={2024},
  volume={},
  number={},
  pages={346-351},
  keywords={Pervasive computing;Privacy;Filtering;Conferences;Computational modeling;Medical services;Chatbots;Large Language Model;Medical Chatbot;Chronic Disease Management},
  doi={10.1109/PerComWorkshops59983.2024.10503257}}
```

### ✨✨ [Try the bot](https://t.me/uniTiro_bot) ✨✨ 

## About the bot

### Features
1. The user can send their measures to the bot. The bot will safely store the data in a database.
2. When asked, the bot is able to provide statistic information (e.g., mean, maximum, minimum) and display historical the data witt plots.
3. The bot also sends reminders to the user to measure his blood pressure and heart rate.

Note: [Telegram](https://telegram.org/) is required to use the bot.

## For Developers

### Good practices
- The project uses the [Gitflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow) workflow.
- The project uses [conventional commits](https://www.conventionalcommits.org/en/v1.0.0/).
- Technical discussions should be done via [issues](https://github.com/LorenzCK/HyperTensionBot/issues).

### How to run 
Clone the repository and open the solution in your preferred IDE.
Configure the following environment variables:
- `SECRET_TELEGRAM_TOKEN`: The token of the Telegram bot. You may want to create a new bot using the [BotFather](https://core.telegram.org/bots#6-botfather).
- `SECRET_KEY_OPENAI`: The key of the OpenAI API if you want to use their models.

You may also change:
- the connection to mongoDB.
- the connection to our models (you need to be pre authenticated and authorized to access them).

## For users
### Setup
1. Start the bot by sending the command `/start` to the bot.
2. The bot will ask you to provide your name and surname. Send your name to the bot.
3. The bot will ask you to provide your birthday. Send your birthday to the bot in the format `dd/mm/yyyy`.
### Chatting with the bot
1. The user can send his measures to the bot by writing them with the following format: `systolic pressure diastolic pressure heart rate`. For example, `120 80 60`. In the future the user may be able to use less strict formats.
2. The user can ask the bot to display his measures (e.g., last measures, all measures, measures in a specific time interval). 
3. The user can also ask general medical information to the bot (e.g., what is hypertension, how to measure blood pressure).
4. If the user does not send his measures for a certain amount of time, the bot will send him a reminder to measure his blood pressure and heart rate.


## Additional information

### Architecture Overview
The system is composed of three main components (plus the user):
- Telegram bot
- Database
- Large Language Model

### Telegram bot
The Telegram bot is the main component of the system.
It is responsible for interacting with the user and for sending him reminders.

### Database
The database is responsible for storing the user's data.
It is also responsible for providing the data to the Telegram bot when requested.

### Large Language Model
It is responsible for interpreting the user's messages and for generating possible answers.

[//]: # (### Communication between components)

[//]: # (Sequence diagram of the communication between the components.)

[//]: # ()
[//]: # (![Communication between components]&#40;https://www.plantuml.com/plantuml/svg/bPF1JiCm38RlUOfmsoJs0fD0e-ouIGW15xI3rVvgAzAqsEvWRqzQWfIozR13Buu_VuxTR4I9jM-SkT4A3MpdgFR6l0hOhkxjOqMQjRgs0b_i8K8b5esKIsFYaLs5jDrklxPVLPrNCUVbrCKyohK-acB8CU6pgxco6fYbAJ0X33crmuczimKfMSPd3z4v_qE0Mp-eRwshHTzf31okQUGNEMlbil29c1eeM19fznUyoZkYHt8zv71cd-YtsTeob6iSiGZpu5FYsMPccyrR6D8xdTGFX84Jn477IVUTQwb8n6b5V2i7_ctiTjpqMOEr5x0cjmgNcchoG6ZjomlBBE71f7Y6TAsNgtycZjpy5bCohv1_H5NA6lX_0nWUnfp9c4s8zutx0G00&#41;)