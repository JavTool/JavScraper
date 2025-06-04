import os
import shutil
import requests
import json
from urllib.parse import urljoin

class JellyfinAPI:
    def __init__(self, server_url, api_key, username=None, password=None):
        """初始化 Jellyfin API 客户端

        Args:
            server_url (str): Jellyfin 服务器地址
            api_key (str): API 密钥
            username (str, optional): 用户名. Defaults to None.
            password (str, optional): 密码. Defaults to None.
        """
        self.server_url = server_url.rstrip('/')
        self.api_key = api_key
        self.username = username
        self.password = password
        self.headers = {
            'X-Emby-Token': api_key,
            'Content-Type': 'application/json'
        }

    def get_folder_image(self, folder_id, image_type='Primary'):
        """获取文件夹的封面图片

        Args:
            folder_id (str): 文件夹ID
            image_type (str, optional): 图片类型. Defaults to 'Primary'.

        Returns:
            bytes: 图片二进制数据
        """
        url = f"{self.server_url}/Items/{folder_id}/Images/{image_type}"
        response = requests.get(url, headers=self.headers)
        if response.status_code == 200:
            return response.content
        return None

    def set_folder_image(self, folder_id, image_path, image_type='Primary'):
        """设置文件夹的封面图片

        Args:
            folder_id (str): 文件夹ID
            image_path (str): 图片文件路径
            image_type (str, optional): 图片类型. Defaults to 'Primary'.

        Returns:
            bool: 是否设置成功
        """
        url = f"{self.server_url}/Items/{folder_id}/Images/{image_type}"
        
        if not os.path.exists(image_path):
            return False
            
        with open(image_path, 'rb') as f:
            files = {'file': f}
            response = requests.post(url, headers=self.headers, files=files)
            return response.status_code == 200

    def get_person_image(self, person_id, image_type='Primary'):
        """获取演员的封面图片

        Args:
            person_id (str): 演员ID
            image_type (str, optional): 图片类型. Defaults to 'Primary'.

        Returns:
            bytes: 图片二进制数据
        """
        url = f"{self.server_url}/Persons/{person_id}/Images/{image_type}"
        response = requests.get(url, headers=self.headers)
        if response.status_code == 200:
            return response.content
        return None

    def set_person_image(self, person_id, image_path, image_type='Primary'):
        """设置演员的封面图片
        http://192.168.1.199:8096/Items/ca3eafe5f769c0fbc28faf88407097e3/Images/Primary

        Args:
            person_id (str): 演员ID
            image_path (str): 图片文件路径
            image_type (str, optional): 图片类型. Defaults to 'Primary'.

        Returns:
            bool: 是否设置成功
        """
        url = f"{self.server_url}/Items/{person_id}/Images/{image_type}"
        
        if not os.path.exists(image_path):
            return False
            
        with open(image_path, 'rb') as f:
            files = {'file': f}
            response = requests.post(url, headers=self.headers, files=files)
            return response.status_code == 200

    def search_folder(self, name):
        """搜索文件夹（精准匹配）

        Args:
            name (str): 文件夹名称

        Returns:
            list: 文件夹列表
        """
        url = f"{self.server_url}/Items"
        params = {
            'IncludeItemTypes': 'Folder',
            'Recursive': 'true',
            'Fields': 'Name'
        }
        response = requests.get(url, headers=self.headers, params=params)
        if response.status_code == 200:
            items = response.json().get('Items', [])
            # 精准匹配文件夹名称
            return [item for item in items if item.get('Name') == name]
        return []

    def search_person(self, name):
        """搜索演员

        Args:
            name (str): 演员名称

        Returns:
            list: 演员列表
        """
        url = f"{self.server_url}/Persons"
        params = {
            'SearchTerm': name
        }
        response = requests.get(url, headers=self.headers, params=params)
        if response.status_code == 200:
            return response.json().get('Items', [])
        return []

    def export_folder_images(self, folder_id, output_dir, image_type='Primary'):
        """导出指定目录下所有子目录的封面图片

        Args:
            folder_id (str): 目录ID
            output_dir (str): 输出目录路径
            image_type (str, optional): 图片类型. Defaults to 'Primary'.

        Returns:
            dict: 导出结果，包含成功和失败的列表
        """
        if not os.path.exists(output_dir):
            os.makedirs(output_dir)

        # 获取所有子目录
        url = f"{self.server_url}/Items"
        params = {
            'ParentId': folder_id,
            'IncludeItemTypes': 'Folder',
            'Recursive': 'true',
            'Fields': 'Path'
        }
        response = requests.get(url, headers=self.headers, params=params)
        
        result = {
            'success': [],
            'failed': []
        }
        
        if response.status_code == 200:
            folders = response.json().get('Items', [])
            for folder in folders:
                folder_name = folder.get('Name')
                folder_path = folder.get('Path')
                if not folder_name or not folder_path:
                    continue
                    
                # 获取封面图片
                image_data = self.get_folder_image(folder['Id'], image_type)
                if image_data:
                    # 使用文件夹名作为图片名
                    image_name = f"{folder_name}.jpg"
                    image_path = os.path.join(output_dir, image_name)
                    
                    try:
                        with open(image_path, 'wb') as f:
                            f.write(image_data)
                        result['success'].append({
                            'name': folder_name,
                            'path': folder_path,
                            'image': image_path
                        })
                    except Exception as e:
                        result['failed'].append({
                            'name': folder_name,
                            'path': folder_path,
                            'error': str(e)
                        })
                else:
                    result['failed'].append({
                        'name': folder_name,
                        'path': folder_path,
                        'error': 'No image found'
                    })
                    
        return result

def main():
    # 使用示例
    api = JellyfinAPI(
        server_url='http://192.168.1.199:8096/',  # 替换为你的 Jellyfin 服务器地址
        api_key='35f5b72e0f734f49bf228f7bf069fc56',              # 替换为你的 API 密钥
        username='Charles Zhang',                    # 可选
        password='Yuexi2018@'                  # 可选
    )

    # 搜索文件夹
    # folders = api.search_folder('Censored')
    # if folders:
    #     folder = folders[0]
    #     # 导出所有子目录的封面
    #     result = api.export_folder_images(folder['Id'], 'D:/folder_covers')
    #     print(f"成功导出: {len(result['success'])} 个")
    #     print(f"导出失败: {len(result['failed'])} 个")

    # 遍历封面目录下的所有图片文件，根据文件名搜索演员并设置封面
    covers_dir = 'D:\\folder_covers'
    if os.path.exists(covers_dir):
        for image_file in os.listdir(covers_dir):
            if image_file.lower().endswith(('.jpg', '.jpeg', '.png')):
                # 使用文件名（不含扩展名）作为演员名称
                actor_name = os.path.splitext(image_file)[0]
                image_path = os.path.join(covers_dir, image_file)
                
                # 搜索演员
                persons = api.search_person(actor_name)
                if persons:
                    person = persons[0]
                    # 设置演员封面
              
                    # success = api.set_person_image(person['Id'], image_path)
                    # if success:
                        # print(f"成功设置演员 {actor_name} 的封面")
                        # 复制图片到指定目录结构
                    first_char = actor_name[0].upper()
                    target_dir = os.path.join('D:\\People', first_char, actor_name)
                    os.makedirs(target_dir, exist_ok=True)
                    target_path = os.path.join(target_dir, 'folder.jpg')
                    try:
                        shutil.copy2(image_path, target_path)
                        print(f"成功复制图片到 {target_path}")
                    except Exception as e:
                        print(f"复制图片失败: {str(e)}")
                    # else:
                    #     print(f"设置演员 {actor_name} 的封面失败")
                else:
                    print(f"未找到演员: {actor_name}")
    else:
        print(f"封面目录不存在: {covers_dir}")

if __name__ == '__main__':
    main()