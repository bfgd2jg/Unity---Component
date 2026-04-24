为了让代码运行起来，你需要确保 Unity 项目中存在以下资源：

文件夹结构： 在 Assets 下创建 Resources/Materials 文件夹。

材质球： * 在上述文件夹内创建两个材质：OutlineMask 和 OutlineFill。

OutlineMask： 建议使用一个简单的 Shader，开启 ColorMask 0 (不输出颜色)，关闭深度写入 ZWrite Off。它的作用是“占位”告诉模板缓冲区这里有物体。

OutlineFill： 这是真正的描边 Shader。它需要读取 UV3 数据（平滑法线），并在顶点着色器中沿该方向挤出。