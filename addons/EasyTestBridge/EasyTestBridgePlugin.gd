@tool
extends EditorPlugin

func _enter_tree() -> void:
	print("[EasyTestBridge] 插件已加载")

func _exit_tree() -> void:
	print("[EasyTestBridge] 插件已卸载")
