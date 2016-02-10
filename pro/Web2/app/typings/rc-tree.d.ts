// Type definitions for react-select

declare module 'rc-tree' {
	import React = require("react");

	interface Tree extends React.ComponentClass<any> { 
		TreeNode: TreeNode;
	}
	const Tree: Tree;

	interface TreeNode extends React.ComponentClass<any> { }
	const TreeNode: TreeNode;

	export = Tree;
}
