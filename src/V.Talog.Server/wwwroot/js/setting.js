let settingBody = {
    "title": "设置",
    "body": [
        {
            title: "删除已保存的查询",
            type: "form",
            api: {
                url: "./setting/query/delete?name=${queryName}&token=" + token,
                method: "POST"
            },
            body: [
                {
                    label: "已保存的查询",
                    type: "select",
                    name: "queryName",
                    source: `./setting/query/list?token=${token}`
                }
            ],
            submitText: "删除",
            reload: "queryName"
        }
    ]
};