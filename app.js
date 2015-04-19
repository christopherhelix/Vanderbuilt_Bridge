var express  =  require("express");
var app = express();
 
var exec = require('child_process').exec;
var cp = require('child_process');
 
var EXEC_PATH = 'node'; //'C:\\Users\\schalert\\Desktop\\MANUAL_OVERRIDE_NODE\\bin\\Debug\\SMS_API_Demo.exe';
var TIMEOUT = 6000;
 
app.get('/', function(req, res) {
  var state = req.query.state;
 
  if(typeof state === 'undefined') {
    return res.json({ error: 'Invalid request' });
  }
 
  var on = (state === 'on');
  var mro = req.query['MRO' + (on ? '1' : '2')];
 
  var proc = exec(EXEC_PATH + ' ' + mro);
 
  setTimeout(function() {
    cp.exec('taskkill /PID ' + proc.pid + ' /T /F', function (error, stdout, stderr) {
      console.log('killed ' + proc.pid);
    });
  }, TIMEOUT);
 
  return res.status(200).end();
});
 
app.listen(3000);