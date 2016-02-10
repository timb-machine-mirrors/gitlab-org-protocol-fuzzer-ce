var _ = require("lodash");
var path = require("path");
var webpack = require("webpack");
var ExtractTextPlugin = require("extract-text-webpack-plugin");

var is_dev = _.some(process.argv, function(item) {
	return item.includes("webpack-dev-server");
});

var entry = {
	app: [ "./app/boot" ],
	vendor: [
		"bootstrap/dist/css/bootstrap.css",
		"font-awesome/css/font-awesome.css",
		"react-bootstrap-table/css/toastr.css",
		"react-bootstrap-table/css/react-bootstrap-table.css",
		"react-widgets/dist/css/react-widgets.css",
		"babel-polyfill",
		"classnames",
		"immutable",
		"lodash",
		"moment",
		"react",
		"react-bootstrap",
		"react-bootstrap-table",
		"react-dom",
		"react-fa",
		"react-redux",
		"react-router5",
		"redux",
		"redux-await",
		"redux-form",
		"redux-router5",
		"router5",
		"router5-history",
		"router5-listeners",
		"superagent"
	]
};

var plugins = [
	new webpack.DefinePlugin({ 
		__DEV__: is_dev,
		'process.env.NODE_ENV': is_dev ? '"development"' : '"production"'
	}),
	new ExtractTextPlugin("[name].css"),
	// new webpack.DllReferencePlugin({
	// 	context: __dirname,
	// 	manifest: require("./assets/vendor-manifest.json")
	// })
	new webpack.optimize.CommonsChunkPlugin({ name: 'vendor', filename: 'vendor.js' })
];

if (is_dev) {
	entry.app.unshift("webpack/hot/dev-server");
	entry.vendor.push('react-hot-api');
	entry.vendor.push('react-hot-loader');
	entry.vendor.push('redux-devtools');
	entry.vendor.push('redux-devtools-log-monitor');
	entry.vendor.push('redux-devtools-dock-monitor');
	plugins.push(new webpack.HotModuleReplacementPlugin());
	plugins.push(new webpack.NoErrorsPlugin());
	plugins.push(new webpack.EvalSourceMapDevToolPlugin());
} else {
	plugins.push(new webpack.SourceMapDevToolPlugin({
		filename: "[file].map",
		exclude: ['vendor.js']
	}));
	plugins.push(new webpack.optimize.UglifyJsPlugin());
	plugins.push(new webpack.optimize.DedupePlugin());
}

var config = {
	cache: true,
	entry: entry,
	output: {
		path: path.join(__dirname, 'dist'),
		filename: '[name].js'
	},
	target: "web",
	resolve: {
		root: path.join(__dirname, "app"),
		extensions: [ "", ".ts", ".tsx", ".js" ],
		alias: {}
	},
	module: {
		loaders: [
			{
				test: /\.js$/,
				exclude: /node_modules/,
				loader: 'babel',
				query: {
					presets: ['es2015', 'react']
				}
			},
			{ test: /\.tsx?$/, loaders: [ 
				"react-hot", 
				"babel?presets[]=es2015&presets[]=react", 
				"ts" 
			]},
			{ test: /\.css(.*)$/,    loader: ExtractTextPlugin.extract("style-loader", "css-loader") },
			{ test: /\.gif$/,        loader: "url-loader?mimetype=image/gif" },
			// handle web fonts
			{ test: /\.eot(.*)$/,    loader: "file" },
			{ test: /\.woff2?(.*)$/, loader: "url?prefix=font/&limit=5000" },
			{ test: /\.otf(.*)$/,    loader: "url?limit=10000&mimetype=application/octet-stream" },
			{ test: /\.tt(.*)$/,     loader: "url?limit=10000&mimetype=application/octet-stream" },
			{ test: /\.svg(.*)$/,    loader: "url?limit=10000&mimetype=image/svg+xml" }
		],
		noParse: []
	},
	debug: is_dev,
	plugins: plugins,
	devServer: {
		port: 9000,
		contentBase: "./assets",
		hot: true,
		historyApiFallback: true,
		proxy: {
			'/p/*': 'http://localhost:8888'
		}
	}
};

module.exports = config;
