var app = angular.module('VotingApp', ['ui.bootstrap']);
app.run(function () { });

app.controller('VotingAppController', ['$rootScope', '$scope', '$http', '$timeout', ($rootScope, $scope, $http, $timeout) => {

    $scope.refresh = () =>
        $http.get('api/Load')
            .then(
                (data, status) => $scope.frequency = data,
                (data, status) => $scope.frequency = undefined);

    $scope.setFrequency = frequency =>
        $http.put('api/Load/' + frequency)
            .then((data, status) => $scope.refresh());
}]);