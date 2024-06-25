define(["jQuery","loading","libraryMenu","fnchecked"],function($,loading,libraryMenu){"use strict";function load(page,config){$("#txtUploadPath",page).val(config.CameraUploadPath||""),$("#chkSubfolder",page).checked(config.EnableCameraUploadSubfolders)}function loadData(page){loading.show(),ApiClient.getNamedConfiguration("devices").then(function(config){load(page,config),loading.hide()})}function save(page){ApiClient.getNamedConfiguration("devices").then(function(config){config.CameraUploadPath=$("#txtUploadPath",page).val(),config.EnableCameraUploadSubfolders=$("#chkSubfolder",page).checked(),ApiClient.updateNamedConfiguration("devices",config).then(Dashboard.processServerConfigurationUpdateResult)})}function onSubmit(){return save($(this).parents(".page")),!1}$(document).on("pageinit","#devicesUploadPage",function(){var page=this;$("#btnSelectUploadPath",page).on("click.selectDirectory",function(){require(["directorybrowser"],function(directoryBrowser){var picker=new directoryBrowser;picker.show({callback:function(path){path&&$("#txtUploadPath",page).val(path),picker.close()},validateWriteable:!0,header:Globalize.translate("HeaderSelectUploadPath")})})}),$(".devicesUploadForm").off("submit",onSubmit).on("submit",onSubmit)}).on("pageshow","#devicesUploadPage",function(){loadData(this)})});