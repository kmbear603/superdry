var itemDB = require("./item-db.js");

const list = itemDB.list();

console.log(list);

const index = Math.floor(Math.random() * list.length);
const item = itemDB.get(list[index]);
console.log(item);

item.name = "test name";
itemDB.set(item);

console.log(itemDB.get(list[index]));
