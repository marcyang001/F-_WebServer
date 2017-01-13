
angular.module('userProfile', [])
    .controller('userController', ['$scope', function ($scope) {
    	
        function refresh(name) {
            
            $.ajax({
                type: "GET", url: "/directchat", dataType: "text",
                success: function (data) {
                    //$("#output").html(data);

                    document.write(data)
                    $("#name").html(name)
                    //var div = document.getElementById('name');
                    //div.innerHTML = name + ": "

                }
            });
        };

        $scope.send = function() {
            var name = $("#textBox").val()
            $.ajax({
                type: "POST", url: "/post",
                dataType: "text",
                data: $("#textBox").val() + " has logged in",
                success: refresh(name)
            });
        }
        
        $scope.runScript = function ($event) {
            if ($event.keyCode == 13) {
                $scope.send();

            }
            return false;
        }
        
    }])