var dv, hilight= true;

function displayDifferences(original, fuzzed)
{

    var target = document.getElementById("view");
    target.innerHTML = "";
    dv = CodeMirror.MergeView(target, {
    value: original,
    origLeft: null ,
    orig: fuzzed,
    lineNumbers: true,
    mode: "text/javascript",
    highlightDifferences: true
  });


    //fixme
  var height;
  var left = $(".CodeMirror-merge-pane").height();
  var right = $(".CodeMirror-merge-pane").next().next().height();
  if(left > right)
    height = left;
  else
    height = right;

  $(".CodeMirror-merge-gap").children("svg").height(height - 6);
  $(".CodeMirror-merge-r-chunk").siblings("pre").children().children().css("font-weight", "bold")
}

function toggleDifferences()
{
  dv.setShowDifferences(hilight = !hilight);
}

function mergeViewHeight(mergeView) {
  function editorHeight(editor) {
   if (!editor) return 0;
    return editor.getScrollInfo().height;
  }

  return Math.max(editorHeight(mergeView.leftOriginal()),
    editorHeight(mergeView.editor()),
    editorHeight(mergeView.rightOriginal()));
}

function resize(mergeView)
{
  var height = mergeViewHeight(mergeView);
  for(;;) {
    if (mergeView.leftOriginal())
     mergeView.leftOriginal().setSize(null, height);

    mergeView.editor().setSize(null, height);

    if (mergeView.rightOriginal())
      mergeView.rightOriginal().setSize(null, height);

    var newHeight = mergeViewHeight(mergeView);
    if (newHeight >= height)
      break;
    else
      height = newHeight;
  }

  mergeView.wrap.style.height = height + "px";
}