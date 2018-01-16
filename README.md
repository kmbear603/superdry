# SuperDry Sales Item Scraper

## Background

The listed price of an item on superdry.com is not necessary to be the final price at checkout. In order to get the final check out price, we have to go through the followings:

1. navigate to item individual webpage
1. select size and add to bag
1. navigate to bag
1. begin checkout
1. select domestic delivery
1. now we have the final checkout price

This application automates the above and present the discounted items in a simple web app.

## Idea of this application

### Stage 1
1. Scrape all discounted category on superdry.com (eg. Mens Sales/Hoodies) and get urls of discounted items
1. for each item, automate the purchase process and record the following information:
    - item name
    - url of item web page
    - url of primary image
    - available sizes
    - color
    - listed price
    - final checkout price
1. store the information of all items in items.json
### Stage 2
1. run a simple web server to host a web app
1. the web app reads items.json generated in Stage 1 and present the items in grid layout

## Module structure
- SuperDry.exe
    - scrape all sales items from superdry.com website and save to items.json
- web app
    - presents items.json to end user in the form of single page web app

## Dependencies
### Scraper
- Selenium WebDriver
- Newtonsoft Json
### Web app
- NodeJS
  - finalhandler
  - serve-static
- Bootstrap 4
- jQuery

## Working environment
### Scraper
- Microsoft Windows
- Chrome browser with version 59 or above +
- chromedriver
### Web app
- NodeJS
- any browser supports HTML5

## How to run
1. execute SuperDry.exe on a Windows machine to get items.json
1. run web server (either locally or on cloud) by the command
    ```javascript
    node server.js
    ```
1. open a web browser and navigate to the website. Note that TCP port 8080 is used by default

## Future works
### Scraper
- port to .Net Core
- resumable scraper job instead of always starting from scratch
- config for category url
- checkout multiple items at once for speed up
- dynamic number of concurrent workers (or use ThreadPool)
- schedule to run every day at 12:00am
- upload items.json automatically after scraping task
### Web app
- use HTML5 history API for client side routing

## Author
kmbear603@gmail.com
