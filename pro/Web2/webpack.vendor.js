var path = require("path");
var webpack = require("webpack");
var ExtractTextPlugin = require("extract-text-webpack-plugin");

module.exports = {
	entry: {
		vendor: [
			"bootstrap/dist/css/bootstrap.css",
			"font-awesome/css/font-awesome.css",
			"react-bootstrap-table/css/toastr.css",
			"react-bootstrap-table/css/react-bootstrap-table.css",
			"babel-polyfill",
			"classnames",
			"lodash",
			"react",
			"react-bootstrap",
			"react-bootstrap-table",
			"react-dom",
			"react-fa",
			"react-redux",
			"react-router5",
			"redux",
			"redux-form",
			"redux-router5",
			"router5",
			"router5-history",
			"router5-listeners",
			"superagent"
		]
	},
	output: {
		path: path.join(__dirname, "assets"),
		filename: "[name].js",
		library: "[name]_[hash]"
	},
	module: {
		loaders: [
			{ test: /\.css(.*)$/,    loader: ExtractTextPlugin.extract("style-loader", "css-loader") },
			// handle web fonts
			{ test: /\.eot(.*)$/,    loader: "file" },
			{ test: /\.woff2?(.*)$/, loader: "url?prefix=font/&limit=5000" },
			{ test: /\.otf(.*)$/,    loader: "url?limit=10000&mimetype=application/octet-stream" },
			{ test: /\.tt(.*)$/,     loader: "url?limit=10000&mimetype=application/octet-stream" },
			{ test: /\.svg(.*)$/,    loader: "url?limit=10000&mimetype=image/svg+xml" }
		]
	},
	plugins: [
		new ExtractTextPlugin("[name].css"),
		new webpack.DllPlugin({
			path: path.join(__dirname, "assets", "[name]-manifest.json"),
			name: "[name]_[hash]"
		})
	]
};
