const os = require('os');
const app = require('app');
const BrowserWindow = require('browser-window');
const spawn = require('child_process').spawn;
const StringDecoder = require('string_decoder').StringDecoder;
const decoder = new StringDecoder('utf8');

// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the javascript object is GCed.
var mainWindow = null;

console.log('type:', os.type());
console.log('platform:', os.platform());
console.log('pwd:', app.getAppPath());

// const cwd = 'C:\\Users\\frank\\src\\peach-pro\\';
// const args = [ '--pits', 'pits/pro' ];

// if (os.platform() === 'win32') {
// 	var cmd = cwd + 'output\\win_x64_debug\\bin\\Peach.exe';
// } else {
// 	var cmd = 'mono';
// 	args = ['--gc=sgen', 'Peach.exe'].concat(args);
// }

// const peach = spawn(cmd, args, { cwd: cwd });

// peach.stdout.on('data', function (data) {
// 	console.log(decoder.write(data));
// });

app.on('window-all-closed', function () {
	app.quit();
});

app.on('quit', function () {
	// peach.kill();
});

app.on('ready', function () {
	mainWindow = new BrowserWindow({ width: 1024, height: 768 });

	mainWindow.webContents.openDevTools();

	setTimeout(function () {
		mainWindow.loadURL('http://localhost:8888/');
	}, 1000);

	mainWindow.on('closed', function () {
		// Dereference the window object, usually you would store windows
		// in an array if your app supports multi windows, this is the time
		// when you should delete the corresponding element.
		mainWindow = null;
	});
});
