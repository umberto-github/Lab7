
    document.getElementById('registrationForm').addEventListener('submit',
        function(event){
            event.preventDefault();
            let pwd = document.getElementById('password').value;
            let confirmPwd = document.getElementById('confirmPassword').value;
            if(pwd != confirmPwd){
                document.getElementById('confirmPassword').setCustomValidity("Password doesn't match");
                //alert("Password doesn't match");
                return false;
            } else {
                document.getElementById('confirmPassword').setCustomValidity("La password è OK !!!");

                //alert("Password OK");
            }
        });

        document.getElementById('confirmPassword').addEventListener('input', function() {
            this.setCustomValidity('');
        });
        


