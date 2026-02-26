@tool
extends EditorPlugin

func _enter_tree() -> void:
	print("[EasyTestBridge] Plugin loaded")

func _exit_tree() -> void:
	print("[EasyTestBridge] Plugin unloaded")
