import cv2
import os
from PIL import Image, ImageDraw
import numpy as np
from pathlib import Path

class FaceCropProcessor:
    def __init__(self):
        """初始化人脸检测器"""
        # 加载 OpenCV 的人脸检测分类器
        self.face_cascade = cv2.CascadeClassifier(cv2.data.haarcascades + 'haarcascade_frontalface_default.xml')
        
    def detect_faces(self, image_path):
        """检测图片中的人脸
        
        Args:
            image_path (str): 图片文件路径
            
        Returns:
            list: 人脸位置列表 [(x, y, w, h), ...]
        """
        # 读取图片
        img = cv2.imread(image_path)
        if img is None:
            print(f"无法读取图片: {image_path}")
            return []
            
        # 转换为灰度图
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        # 检测人脸
        faces = self.face_cascade.detectMultiScale(
            gray,
            scaleFactor=1.1,
            minNeighbors=5,
            minSize=(30, 30)
        )
        
        return faces.tolist() if len(faces) > 0 else []
    
    def calculate_crop_area(self, image_size, faces, target_ratio=3/2):
        """计算裁切区域
        
        Args:
            image_size (tuple): 原图尺寸 (width, height)
            faces (list): 人脸位置列表
            target_ratio (float): 目标宽高比 (height/width)
            
        Returns:
            tuple: 裁切区域 (left, top, right, bottom)
        """
        width, height = image_size
        
        if not faces:
            # 没有检测到人脸，从中心裁切
            target_width = min(width, height / target_ratio)
            target_height = target_width * target_ratio
            
            left = (width - target_width) // 2
            top = (height - target_height) // 2
            right = left + target_width
            bottom = top + target_height
            
            return (int(left), int(top), int(right), int(bottom))
        
        # 找到所有人脸的边界框
        min_x = min(face[0] for face in faces)
        min_y = min(face[1] for face in faces)
        max_x = max(face[0] + face[2] for face in faces)
        max_y = max(face[1] + face[3] for face in faces)
        
        # 计算人脸区域的中心点
        face_center_x = (min_x + max_x) // 2
        face_center_y = (min_y + max_y) // 2
        
        # 计算目标裁切尺寸
        target_width = min(width, height / target_ratio)
        target_height = target_width * target_ratio
        
        # 以人脸中心为基准计算裁切区域
        left = face_center_x - target_width // 2
        top = face_center_y - target_height // 2
        
        # 确保裁切区域在图片范围内
        left = max(0, min(left, width - target_width))
        top = max(0, min(top, height - target_height))
        
        right = left + target_width
        bottom = top + target_height
        
        return (int(left), int(top), int(right), int(bottom))
    
    def crop_image(self, image_path, output_path=None, target_ratio=3/2):
        """裁切图片
        
        Args:
            image_path (str): 输入图片路径
            output_path (str): 输出图片路径，如果为 None 则覆盖原文件
            target_ratio (float): 目标宽高比 (height/width)
            
        Returns:
            bool: 是否成功
        """
        try:
            # 检测人脸
            faces = self.detect_faces(image_path)
            print(f"在 {os.path.basename(image_path)} 中检测到 {len(faces)} 个人脸")
            
            # 如果没有检测到人脸，跳过裁切
            if len(faces) == 0:
                print(f"未检测到人脸，跳过裁切: {os.path.basename(image_path)}")
                return True
            
            # 打开图片
            with Image.open(image_path) as img:
                # 计算裁切区域
                crop_area = self.calculate_crop_area(img.size, faces, target_ratio)
                
                # 裁切图片
                cropped_img = img.crop(crop_area)
                
                # 保存图片
                if output_path is None:
                    output_path = image_path
                    
                cropped_img.save(output_path, quality=95)
                print(f"已保存裁切后的图片: {output_path}")
                print(f"原尺寸: {img.size}, 裁切后尺寸: {cropped_img.size}")
                
                return True
                
        except Exception as e:
            print(f"处理图片 {image_path} 时出错: {e}")
            return False
    
    def process_directory(self, directory_path, output_dir=None, target_ratio=3/2):
        """批量处理目录中的图片
        
        Args:
            directory_path (str): 输入目录路径
            output_dir (str): 输出目录路径，如果为 None 则覆盖原文件
            target_ratio (float): 目标宽高比 (height/width)
        """
        supported_formats = {'.jpg', '.jpeg', '.png', '.bmp', '.tiff'}
        
        directory = Path(directory_path)
        if not directory.exists():
            print(f"目录不存在: {directory_path}")
            return
            
        if output_dir:
            output_directory = Path(output_dir)
            output_directory.mkdir(parents=True, exist_ok=True)
        
        processed_count = 0
        success_count = 0
        
        for image_file in directory.iterdir():
            if image_file.suffix.lower() in supported_formats:
                processed_count += 1
                
                if output_dir:
                    output_path = output_directory / image_file.name
                else:
                    output_path = None
                    
                if self.crop_image(str(image_file), str(output_path) if output_path else None, target_ratio):
                    success_count += 1
                    
        print(f"\n处理完成: 共处理 {processed_count} 张图片，成功 {success_count} 张")

def main():
    """主函数"""
    # 固定路径配置
    FIXED_INPUT_PATH = r"\\192.168.1.199\Porn\Uncensored\前田かおり\[2015-01-01] - [010115-001] - [THE 未公開 ～前田かおりの３電マショック～]\folder.jpg"  # 输入目录路径

    OUTPUT_PATH = None  # 输出路径，None 表示覆盖原文件
    TARGET_RATIO = 1.5  # 目标宽高比 (height/width)，1.5 表示 3:2
    
    processor = FaceCropProcessor()
    
    input_path = Path(FIXED_INPUT_PATH)
    
    if input_path.is_file():
        # 处理单个文件
        processor.crop_image(str(input_path), OUTPUT_PATH, TARGET_RATIO)
    elif input_path.is_dir():
        # 处理目录
        processor.process_directory(str(input_path), OUTPUT_PATH, TARGET_RATIO)
    else:
        print(f"输入路径不存在: {FIXED_INPUT_PATH}")

if __name__ == "__main__":
    main()