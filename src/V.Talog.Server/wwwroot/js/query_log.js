let token = localStorage.token;

let queryLogBody = {
    "title": "日志查询",
    "body": [
        {
            "type": "form",
            name: "query",
            "title": "搜索",
            "mode": "horizontal",
            data: {
                index: null
            },
            "body": [
                {
                    "label": "Index",
                    "type": "select",
                    "name": "index",
                    source: `./log/index/list?token=${token}`,
                    required: true
                },
                {
                    label: "标签查询",
                    "type": "input-text",
                    "name": "tagQuery",
                    required: true
                },
                {
                    label: "正则表达式",
                    "type": "input-text",
                    "name": "regex",
                    placeholder: "用于匹配日志"
                },
                {
                    label: "字段查询",
                    "type": "input-text",
                    "name": "fieldQuery",
                    placeholder: "若日志为 json 格式或使用正则匹配时，可根据字段进行进一步查询"
                }
            ],
            actions: [
                {
                    type: "button",
                    label: "查询",
                    level: "primary",
                    actionType: "reload",
                    target: "logs?index=${index}&tagQuery=${tagQuery}&regex=${regex}&fieldQuery=${fieldQuery}"
                }
            ]
        },
        {
            type: "crud",
            name: "logs",
            api: {
                url: "./log/search",
                method: "POST",
                data: {
                    index: "${index}",
                    tagQuery: "${tagQuery}",
                    regex: "${regex}",
                    fieldQuery: "${fieldQuery}"
                },
                adaptor: function (payload, response) {
                    return {
                        msg: payload.msg,
                        status: payload.code,
                        data: {
                            count: payload.data.length,
                            rows: payload.data
                        }
                    };
                }
            },
            mode: "list",
            syncLocation: false,
            initFetch: false,
            listItem: {
                "body": [
                    "${data}",
                    {
                        type: "each",
                        source: "${tags}",
                        items: {
                            type: "tpl",
                            tpl: "<span class='label label-info m-l-sm'><%= data.label %>: <%= data.value %></span>"
                        }
                    },
                    {
                        type: "each",
                        source: "${groups | objectToArray}",
                        items: {
                            type: "tpl",
                            tpl: "<span class='label label-default m-l-sm'><%= data.label %>: <%= data.value %></span>"
                        }
                    }
                ]
            }
        }
    ]
};